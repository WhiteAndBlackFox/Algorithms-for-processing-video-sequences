namespace AForge.Video.DirectShow
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading;
    using System.Runtime.InteropServices;

    using Video;
    using Internals;

    public class FileVideoSource : IVideoSource
    {
        // video file name
        private string _fileName;
        private long _duration;
        // received frames count
        private int _framesReceivedFromLastTime;
        // received frames count
        private int _framesReceived;
        // recieved byte count
        private long _bytesReceived;
        // prevent freezing
        private bool _preventFreezing;
        // reference clock for the graph - when disabled, graph processes frames ASAP
        private bool _referenceClockEnabled = true;

        // TIME_FORMAT_MEDIA_TIME (100-nanosecond units).
        private const int OneSecond = 10000000;
        public bool IsSetPause;
        public bool IsSetPlay;

        public bool IsPlaying {
            get;
            private set;
        }
        // seeking capability
        private bool _isSeekEnabled;
        // seeking function flags
        private bool _isSetCurrentTime;
        private bool _isGetCurrentTime;
        // seeking function params
        private long _currentSetTime;
        private long _currentGetTime;

        private Thread _thread = null;
        private ManualResetEvent _stopEvent = null;
        private readonly Action<long> _onVideoLoad;

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>Notifies clients about new available frame from video source.</para>
        /// 
        /// <para><note>Since video source may have multiple clients, each client is responsible for
        /// making a copy (cloning) of the passed video frame, because the video source disposes its
        /// own original copy after notifying of clients.</note></para>
        /// </remarks>
        /// 
        public event NewFrameEventHandler NewFrame;

        /// <summary>
        /// Video source error event.
        /// </summary>
        /// 
        /// <remarks>This event is used to notify clients about any type of errors occurred in
        /// video source object, for example internal exceptions.</remarks>
        /// 
        public event VideoSourceErrorEventHandler VideoSourceError;

        /// <summary>
        /// Video playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the video playing has finished.</para>
        /// </remarks>
        /// 
        public event PlayingFinishedEventHandler PlayingFinished;

        /// <summary>
        /// Video source.
        /// </summary>
        /// 
        /// <remarks>Video source is represented by video file name.</remarks>
        /// 
        public virtual string Source
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        /// <summary>
        /// Received frames count.
        /// </summary>
        /// 
        /// <remarks>Number of frames the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public int FramesReceivedFromLastTime {
            get {
                int frames = _framesReceivedFromLastTime;
                _framesReceivedFromLastTime = 0;
                return frames;
            }
        }

        public int FramesReceived {
            get {
                return _framesReceived;
            }
        }

        /// <summary>
        /// Received bytes count.
        /// </summary>
        /// 
        /// <remarks>Number of bytes the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public long BytesReceived
        {
            get
            {
                long bytes = _bytesReceived;
                _bytesReceived = 0;
                return bytes;
            }
        }

        /// <summary>
        /// State of the video source.
        /// </summary>
        /// 
        /// <remarks>Current state of video source object - running or not.</remarks>
        /// 
        public bool IsRunning
        {
            get
            {
                if (_thread != null)
                {
                    // check thread status
                    if (_thread.Join(0) == false)
                        return true;

                    // the thread is not running, free resources
                    Free();
                }
                return false;
            }
        }

        /// <summary>
        /// Prevent video freezing after screen saver and workstation lock or not.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>The value specifies if the class should prevent video freezing during and
        /// after screen saver or workstation lock. To prevent freezing the <i>DirectShow</i> graph
        /// should not contain <i>Renderer</i> filter, which is added by <i>Render()</i> method
        /// of graph. However, in some cases it may be required to call <i>Render()</i> method of graph, since
        /// it may add some more filters, which may be required for playing video. So, the property is
        /// a trade off - it is possible to prevent video freezing skipping adding renderer filter or
        /// it is possible to keep renderer filter, but video may freeze during screen saver.</para>
        /// 
        /// <para><note>The property may become obsolete in the future when approach to disable freezing
        /// and adding all required filters is found.</note></para>
        /// 
        /// <para><note>The property should be set before calling <see cref="Start"/> method
        /// of the class to have effect.</note></para>
        /// 
        /// <para>Default value of this property is set to <b>false</b>.</para>
        /// 
        /// </remarks>
        /// 
        public bool PreventFreezing
        {
            get { return _preventFreezing; }
            set { _preventFreezing = value; }
        }

        /// <summary>
        /// Enables/disables reference clock on the graph.
        /// </summary>
        /// 
        /// <remarks><para>Disabling reference clocks causes DirectShow graph to run as fast as
        /// it can process data. When enabled, it will process frames according to presentation
        /// time of a video file.</para>
        /// 
        /// <para><note>The property should be set before calling <see cref="Start"/> method
        /// of the class to have effect.</note></para>
        /// 
        /// <para>Default value of this property is set to <b>true</b>.</para>
        /// </remarks>
        /// 
        public bool ReferenceClockEnabled
        {
            get { return _referenceClockEnabled; }
            set { _referenceClockEnabled = value;}
        }

        public long Duration
        {
            get { return _duration; }
        }

        /// <summary>
        /// Used to check if seeking is supported.
        /// </summary>
        public bool IsSeekEnabled
        {
            get { return _isSeekEnabled; }
        }

        public FileVideoSource()
        {
            
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FileVideoSource"/> class.
        /// </summary>
        /// 
        /// <param name="fileName">Video file name.</param>
        /// <param name="onVideoLoad"></param>
        public FileVideoSource(string fileName, Action<long> onVideoLoad)
        {
            _fileName = fileName;
            _onVideoLoad = onVideoLoad;
            _duration = 0;
        }

        /// <summary>
        /// Start video source.
        /// </summary>
        /// 
        /// <remarks>Starts video source and return execution to caller. Video source
        /// object creates background thread and notifies about new frames with the
        /// help of <see cref="NewFrame"/> event.</remarks>
        /// 
        public void Start()
        {
            if (!IsRunning)
            {
                // check source
                if ((_fileName == null) || (_fileName == string.Empty))
                    throw new ArgumentException("Video source is not specified");

                _framesReceived = 0;
                _framesReceivedFromLastTime = 0;
                _bytesReceived = 0;

                // create events
                _stopEvent = new ManualResetEvent(false);

                // create and start new thread
                _thread = new Thread(WorkerThread)
                {
                    Name = _fileName
                };
                // mainly for debugging
                _thread.Start();
            }
        }

        /// <summary>
        /// Signal video source to stop its work.
        /// </summary>
        /// 
        /// <remarks>Signals video source to stop its background thread, stop to
        /// provide new frames and free resources.</remarks>
        /// 
        public void SignalToStop()
        {
            // stop thread
            if (_thread != null)
            {
                // signal to stop
                _stopEvent.Set();
            }
        }

        /// <summary>
        /// Wait for video source has stopped.
        /// </summary>
        /// 
        /// <remarks>Waits for source stopping after it was signalled to stop using
        /// <see cref="SignalToStop"/> method.</remarks>
        /// 
        public void WaitForStop()
        {
            if (_thread != null)
            {
                // wait for thread stop
                _thread.Join();

                Free();
            }
        }

        /// <summary>
        /// Stop video source.
        /// </summary>
        /// 
        /// <remarks><para>Stops video source aborting its thread.</para>
        /// 
        /// <para><note>Since the method aborts background thread, its usage is highly not preferred
        /// and should be done only if there are no other options. The correct way of stopping camera
        /// is <see cref="SignalToStop">signaling it stop</see> and then
        /// <see cref="WaitForStop">waiting</see> for background thread's completion.</note></para>
        /// </remarks>
        /// 
        public void Stop()
        {
            if (IsRunning)
            {
                try
                {
                    _thread.Abort();
                    WaitForStop();
                }
                catch (Exception e)
                {
                    //Ignoring
                }
            }
        }

        /// <summary>
        /// Retrieves the current time (seconds) in terms of the total time of the media stream
        /// </summary>
        /// <returns>The current stream time</returns>
        public uint GetCurrentTime()
        {
            if (_thread == null || !IsRunning || !_isSeekEnabled)
                return 0;

            _isGetCurrentTime = true;
            return Convert.ToUInt32(_currentGetTime / OneSecond);
        }

        /// <summary>
        /// Sets the current time (seconds) in terms of the total time of the media stream
        /// </summary>
        /// <param name="currentSeconds">The current stream time</param>
        public void SetCurrentTime(long currentSeconds)
        {
            if (_thread == null || !IsRunning || !_isSeekEnabled)
                return;

            _currentSetTime = currentSeconds * OneSecond;
            _isSetCurrentTime = true;
        }

        /// <summary>
        /// Free resource.
        /// </summary>
        /// 
        private void Free()
        {
            _thread = null;

            // release events
            _stopEvent.Close();
            _stopEvent = null;
        }

        /// <summary>
        /// Worker thread.
        /// </summary>
        /// 
        private void WorkerThread()
        {
            ReasonToFinishPlaying reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            // grabber
            Grabber grabber = new Grabber(this);

            // objects
            object graphObject = null;
            object grabberObject = null;

            // interfaces
            IGraphBuilder       graph = null;
            IBaseFilter         sourceBase = null;
            IBaseFilter         grabberBase = null;
            ISampleGrabber      sampleGrabber = null;
            IMediaControl       mediaControl = null;

            IMediaEventEx       mediaEvent = null;
            IMediaSeeking       mediaSeeking = null;

            try
            {
                // get type for filter graph
                Type type = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (type == null)
                    throw new ApplicationException("Failed creating filter graph");

                // create filter graph
                graphObject = Activator.CreateInstance(type);
                graph = (IGraphBuilder) graphObject;

                // create source device's object
                graph.AddSourceFilter(_fileName, "source", out sourceBase);
                if (sourceBase == null)
                    throw new ApplicationException("Failed creating source filter");

                // get type for sample grabber
                type = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (type == null)
                    throw new ApplicationException("Failed creating sample grabber");

                // create sample grabber
                grabberObject = Activator.CreateInstance(type);
                sampleGrabber = (ISampleGrabber) grabberObject;
                grabberBase = (IBaseFilter) grabberObject;

                // add grabber filters to graph
                graph.AddFilter(grabberBase, "grabber");

                // set media type
                AMMediaType mediaType = new AMMediaType
                {
                    MajorType = MediaType.Video,
                    SubType = MediaSubType.RGB24
                };
                sampleGrabber.SetMediaType(mediaType);

                // connect pins
                int pinToTry = 0;

                IPin inPin = Tools.GetInPin(grabberBase, 0);
                IPin outPin = null;

                // find output pin acceptable by sample grabber
                while (true)
                {
                    outPin = Tools.GetOutPin(sourceBase, pinToTry);

                    if (outPin == null)
                    {
                        Marshal.ReleaseComObject(inPin);
                        throw new ApplicationException("Did not find acceptable output video pin in the given source");
                    }

                    if (graph.Connect(outPin, inPin) < 0)
                    {
                        Marshal.ReleaseComObject(outPin);
                        outPin = null;
                        pinToTry++;
                    }
                    else
                    {
                        break;
                    }
                }

                Marshal.ReleaseComObject(outPin);
                Marshal.ReleaseComObject(inPin);

                // get media type
                if (sampleGrabber.GetConnectedMediaType(mediaType) == 0)
                {
                    VideoInfoHeader vih = (VideoInfoHeader) Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));

                    grabber.Width = vih.BmiHeader.Width;
                    grabber.Height = vih.BmiHeader.Height;
                    mediaType.Dispose();
                }

                // let's do rendering, if we don't need to prevent freezing
                if (!_preventFreezing)
                {
                    // render pin
                    graph.Render(Tools.GetOutPin(grabberBase, 0));

                    // configure video window
                    IVideoWindow window = (IVideoWindow) graphObject;
                    window.put_AutoShow(false);
                    window = null;
                }

                // configure sample grabber
                sampleGrabber.SetBufferSamples(false);
                sampleGrabber.SetOneShot(false);
                sampleGrabber.SetCallback(grabber, 1);

                // disable clock, if someone requested it
                if (!_referenceClockEnabled)
                {
                    IMediaFilter mediaFilter = (IMediaFilter) graphObject;
                    mediaFilter.SetSyncSource(null);
                }

                // get media control
                mediaControl = (IMediaControl) graphObject;

                // get media events' interface
                mediaEvent = (IMediaEventEx) graphObject;

                // Get media seeking & check seeking capability
                mediaSeeking = (IMediaSeeking)graphObject;
                mediaSeeking.GetDuration(out _duration);
                _onVideoLoad(_duration);
                const SeekingCapabilities caps = SeekingCapabilities.CanSeekAbsolute | SeekingCapabilities.CanGetDuration;
                SeekingCapabilities canSeekCap;
                int hr = mediaSeeking.GetCapabilities(out canSeekCap);
                if(hr < 0)
                    throw new ApplicationException("Failed getting seeking capabilities");
                _isSeekEnabled = (canSeekCap & caps) == caps;

                // run
                mediaControl.Run();
                IsPlaying = true;
                do
                {
                    // GetCurrentTime
                    if (_isGetCurrentTime)
                    {
                        mediaSeeking.GetCurrentPosition(out _currentGetTime);
                        _isGetCurrentTime = false;
                    }
                    if (IsSetPause)
                    {
                        mediaControl.Pause();
                        IsSetPause = false;
                        IsPlaying = false;
                    }
                    if (IsSetPlay) {
                        mediaControl.Run();
                        IsSetPlay = false;
                        IsPlaying = true;
                    }
                    // SetCurrentTime
                    if (_isSetCurrentTime)
                    {
                        long stop = 0;
                        mediaSeeking.SetPositions(ref _currentSetTime, SeekingFlags.AbsolutePositioning, ref stop,
                            SeekingFlags.NoPositioning);
                        _isSetCurrentTime = false;
                    }
                    IntPtr p1;
                    IntPtr p2;
                    DsEvCode code;
                    if (mediaEvent.GetEvent(out code, out p1, out p2, 0) >= 0)
                    {
                        mediaEvent.FreeEventParams(code, p1, p2);

                        if (code == DsEvCode.Complete)
                        {
                            reasonToStop = ReasonToFinishPlaying.EndOfStreamReached;
                            break;
                        }
                    }
                } while (!_stopEvent.WaitOne(100, false));
                IsPlaying = false;
                mediaControl.Stop();
            }
            catch (Exception exception)
            {
                // provide information to clients
                if (VideoSourceError != null)
                {
                    VideoSourceError(this, new VideoSourceErrorEventArgs(exception.Message));
                }
            }
            finally
            {
                // release all objects
                graph           = null;
                grabberBase     = null;
                sampleGrabber   = null;
                mediaControl    = null;
                mediaEvent      = null;
                mediaSeeking    = null;

                if (graphObject != null)
                {
                    Marshal.ReleaseComObject(graphObject);
                    graphObject = null;
                }
                if (sourceBase != null)
                {
                    Marshal.ReleaseComObject(sourceBase);
                    sourceBase = null;
                }
                if (grabberObject != null)
                {
                    Marshal.ReleaseComObject(grabberObject);
                    grabberObject = null;
                }
            }

            if (PlayingFinished != null)
            {
                PlayingFinished(this, reasonToStop);
            }
        }

        /// <summary>
        /// Notifies client about new frame.
        /// </summary>
        /// 
        /// <param name="image">New frame's image.</param>
        /// 
        protected void OnNewFrame(Bitmap image)
        {
            _framesReceived++;
            _framesReceivedFromLastTime++;
            _bytesReceived += image.Width * image.Height * (Bitmap.GetPixelFormatSize(image.PixelFormat) >> 3);

            if ((!_stopEvent.WaitOne(0, false)) && (NewFrame != null))
                NewFrame(this, new NewFrameEventArgs(image));
        }

        //
        // Video grabber
        //
        private class Grabber : ISampleGrabberCB
        {
            private readonly FileVideoSource _parent;
            private int _width, _height;

            // Width property
            public int Width
            {
                get { return _width; }
                set { _width = value; }
            }
            // Height property
            public int Height
            {
                get { return _height; }
                set { _height = value; }
            }

            // Constructor
            public Grabber(FileVideoSource parent)
            {
                this._parent = parent;
            }

            // Callback to receive samples
            public int SampleCB(double sampleTime, IntPtr sample)
            {
                return 0;
            }

            // Callback method that receives a pointer to the sample buffer
            public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
            {
                if (_parent.NewFrame != null)
                {
                    // create new image
                    System.Drawing.Bitmap image = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);

                    // lock bitmap data
                    BitmapData imageData = image.LockBits(
                        new Rectangle(0, 0, _width, _height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format24bppRgb);

                    // copy image data
                    int srcStride = imageData.Stride;
                    int dstStride = imageData.Stride;

                    unsafe
                    {
                        byte* dst = (byte*) imageData.Scan0.ToPointer() + dstStride * (_height - 1);
                        byte* src = (byte*) buffer.ToPointer();

                        for (int y = 0; y < _height; y++)
                        {
                            Win32.memcpy(dst, src, srcStride);
                            dst -= dstStride;
                            src += srcStride;
                        }
                    }

                    // unlock bitmap data
                    image.UnlockBits(imageData);

                    // notify parent
                    _parent.OnNewFrame(image);

                    // release the image
                    image.Dispose();
                }

                return 0;
            }
        }
    }
}
