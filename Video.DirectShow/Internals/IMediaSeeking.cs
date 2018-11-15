namespace AForge.Video.DirectShow.Internals
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// The IMediaSeeking interface Contains methods for seeking to a position within a stream,
    /// and for setting the playback rate
    /// </summary>
	[ComImport,
	Guid("36B73880-C2C8-11CF-8B46-00805F6CEF60"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IMediaSeeking
	{
		/// <summary>
        /// Retrieves all the seeking capabilities of the stream
		/// </summary>
		[PreserveSig]
		int GetCapabilities(
			out SeekingCapabilities pCapabilities);

		/// <summary>
        /// Queries whether a stream has specified seeking capabilities
		/// </summary>
		[PreserveSig]
		int CheckCapabilities(
			[In, Out] ref SeekingCapabilities pCapabilities);

		/// <summary>
        /// Determines whether a specified time format is supported
		/// </summary>
		[PreserveSig]
		int IsFormatSupported(
			[In] ref Guid pFormat);

		/// <summary>
        /// Retrieves the preferred time format for the stream
		/// </summary>
		[PreserveSig]
		int QueryPreferredFormat(
			[Out] out Guid pFormat);

		/// <summary>
        /// Retrieves the current time format
		/// </summary>
		[PreserveSig]
		int GetTimeFormat(
			[Out] out Guid pFormat);

		/// <summary>
        /// Determines whether a specified time format is the format currently in use
		/// </summary>
		[PreserveSig]
		int IsUsingTimeFormat([In] ref Guid pFormat);

		/// <summary>
        /// Sets the time format
		/// </summary>
		[PreserveSig]
		int SetTimeFormat([In] ref Guid pFormat);

		/// <summary>
        /// Retrieves the duration of the stream
		/// </summary>
		[PreserveSig]
		int GetDuration(out long pDuration);

		/// <summary>
        /// Retrieves the time at which the playback will stop, relative to the duration of the stream
		/// </summary>
		[PreserveSig]
		int GetStopPosition(out long pStop);

		/// <summary>
        /// Retrieves the current position, relative to the total duration of the stream
		/// </summary>
		[PreserveSig]
		int GetCurrentPosition(out long pCurrent);

		/// <summary>
        /// Converts from one time format to another
		/// </summary>
		[PreserveSig]
		int ConvertTimeFormat(
			out long pTarget,
			[In] ref Guid pTargetFormat,
			long source, 
			[In] ref Guid pSourceFormat);

		/// <summary>
        /// Sets the current position and the stop position
		/// </summary>
		[PreserveSig]
		int SetPositions(
			[In, Out] ref long pCurrent,
			SeekingFlags dwCurrentFlags,
			[In, Out] ref long pStop,
			SeekingFlags dwStopFlags);

		/// <summary>
        /// Retrieves the current position and the stop position, relative to the total duration of the stream
		/// </summary>
		[PreserveSig]
		int GetPositions(out long pCurrent, out long pStop);

		/// <summary>
        /// Retrieves the range of times in which seeking is efficient
		/// </summary>
		[PreserveSig]
		int GetAvailable(out long pEarliest, out long pLatest);

		/// <summary>
        /// Sets the playback rate
		/// </summary>
		[PreserveSig]
		int SetRate(double dRate);

		/// <summary>
        /// Retrieves the playback rate
		/// </summary>
		[PreserveSig]
		int GetRate(out double pdRate);

		/// <summary>
        /// Retrieves the amount of data that will be queued before the start position
		/// </summary>
		[PreserveSig]
		int GetPreroll(out long pllPreroll);
	}
}