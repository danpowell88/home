using daemonapp.apps.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace daemonapp.apps.SpeechEngine
{
    public class SpeechEngine
    {
        List<string> MorningGreetings = new List<string> { "Good Morning", "Morning" };
        List<string> DayGreetings = new List<string> { "Good Day" };
        List<string> EveningGreetings = new List<string> { "Good Evening", "Evening" };

        List<string> AllDayGreetings = new List<string> { "Hi", "Hello"  };

        string Greeting =>
            DateTime.Now.Hour >= 5 && DateTime.Now.Hour <= 12 ?
                MorningGreetings.Union(AllDayGreetings).Random() :
                    DateTime.Now.Hour > 12 && DateTime.Now.Hour <= 4 ?
                        DayGreetings.Union(AllDayGreetings).Random() :
                        DateTime.Now.Hour > 4 && DateTime.Now.Hour <= 10 ?
                        EveningGreetings.Union(AllDayGreetings).Random() :
                        AllDayGreetings.Random();

        public string Generate(string message, bool greeting, string? subjectName = null)
        {
            var s = new StringBuilder();
            if (greeting)
            {
                s.Append(Greeting);

                if (subjectName != null))
                {
                    s.Append(subjectName);
                    s.Append(",");
                }
            }

            s.Append(message);
        }
            
    }
}
