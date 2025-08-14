using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeLineTest
{
    public class RecordingBar
    {
        public class HourMask
        {
            public long utcTime { get; set; }

            public DateTime dateTime
            {
                get => DateTimeOffset.FromUnixTimeSeconds((long)utcTime).ToLocalTime().DateTime;
                set => utcTime = ((DateTimeOffset)value).ToUnixTimeSeconds();
            }

            public MinuteMask[] min { get; } = new MinuteMask[60];
            public HourMask()
            {
                for (int i = 0; i < 60; i++)
                    min[i] = new MinuteMask();
            }
        }

        public class MinuteMask
        {
            public SecondMask[] sec { get; } = new SecondMask[60];
            public MinuteMask()
            {
                for (int i = 0; i < 60; i++)
                    sec[i] = new SecondMask();
            }
        }

        public class SecondMask
        {
            public long mask;
            public char tcDisk;
            public int fileNameTime;
            public int fileOffset;
            public int size;
        }



        public readonly struct RecordingSegment
        {
            public DateTime Start { get; }
            public DateTime End { get; }
            public int EventMask { get; }

            public TimeSpan Duration => End - Start;

            public RecordingSegment(DateTime start, DateTime end, int eventMask)
            {
                if (end <= start)
                    throw new ArgumentException("End must be greater than Start.", nameof(end));

                Start = start;
                End = end;
                EventMask = eventMask;
            }

            public bool Has(int flag) => (EventMask & flag) != 0;
        }
    }
}
