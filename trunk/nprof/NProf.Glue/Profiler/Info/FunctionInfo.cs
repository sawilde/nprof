using System;

namespace NProf.Glue.Profiler.Info
{
	/// <summary>
	/// Summary description for FunctionInfo.
	/// </summary>
	public class FunctionInfo
	{
		public FunctionInfo( ThreadInfo ti, int nID, FunctionSignature fs, int nCalls, long lTotalTime, long lTotalSuspendedTime, CalleeFunctionInfo[] acfi )
		{
			_ti = ti;
			_nID = nID;
			_fs = fs;
			_nCalls = nCalls;
			_lTotalTime = lTotalTime;
			_lTotalSuspendedTime = lTotalSuspendedTime;
			_acfi = acfi;

			foreach ( CalleeFunctionInfo cfi in _acfi )
				cfi.FunctionInfo = this;
		}

		public int ID
		{
			get { return _nID; }
		}

		public FunctionSignature Signature
		{
			get { return _fs; }
		}

		public CalleeFunctionInfo[] CalleeInfo
		{
			get { return _acfi; }
		}

		public int Calls
		{
			get { return _nCalls; }
		}

		public long ThreadTotalTicks
		{
			get { return _ti.TotalTime; }
		}

		public long TotalTicks
		{
			get { return _lTotalTime; }
		}

		public long TotalSuspendedTicks
		{
			get { return _lTotalSuspendedTime; }
		}

		/// <summary>
		/// Percent on time, based on thread ticks, that method is suspended.
		/// </summary>
		public double PercentOfTotalTimeSuspended
		{
			get 
			{
				if ( ThreadTotalTicks == 0 )
					return 0;

				return ( ( double )_lTotalSuspendedTime / ( double )ThreadTotalTicks ) * 100;
			}
		}

		/// <summary>
		/// Percent on time, based on method ticks (not thread ticks), that method is suspended.
		/// </summary>
		public double PercentOfTimeSuspended
		{
			get 
			{
				if ( TotalTicks == 0 )
					return 0;

				return ( ( double )_lTotalSuspendedTime / ( double )TotalTicks ) * 100;
			}
		}
		
		/// <summary>
		/// Percent of time, based on total thread ticks, spent in method (not including children).
		/// </summary>
		public double PercentOfTotalTimeInMethod
		{
			get
			{
				if ( ThreadTotalTicks == 0 )
					return 0;

				long lTotalChildrenTime = 0;
				foreach ( CalleeFunctionInfo cfi in _acfi )
					lTotalChildrenTime += cfi.TotalTime;

				return ( ( ( double )_lTotalTime - ( double )lTotalChildrenTime ) / ( double )ThreadTotalTicks ) * 100;
			}
		}

		/// <summary>
		/// Percent of time, based on total thread ticks, spent in method and children.
		/// </summary>
		public double PercentOfTotalTimeInMethodAndChildren
		{
			get
			{
				if ( ThreadTotalTicks == 0 )
					return 0;

				return ( ( double )_lTotalTime / ( double )ThreadTotalTicks ) * 100;
			}
		}

		/// <summary>
		/// Percent of time, based on method ticks (not thread ticks), spent in children.
		/// </summary>
		public double PercentOfTimeInChildren
		{
			get
			{
				if ( TotalTicks == 0 )
					return 0;

				long lTotalChildrenTime = 0;
				foreach ( CalleeFunctionInfo cfi in _acfi )
					lTotalChildrenTime += cfi.TotalTime;

				return ( ( double )lTotalChildrenTime / ( double )TotalTicks ) * 100;
			}
		}

		private int _nID;
		private int _nCalls;
		private long _lTotalTime;
		private long _lTotalSuspendedTime;
		private FunctionSignature _fs;
		private CalleeFunctionInfo[] _acfi;
		private ThreadInfo _ti;
	}
}
