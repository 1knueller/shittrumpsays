using BrightWire;
using BrightWire.TrainingData;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ShitTrumpSays
{
    public class MarkovTrump
    {
        public static void MarkovChains()
        {
            var lines = res.trump.Split('\n');

            StringBuilder sb = new StringBuilder();
            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l))
                    continue;

                var spl = l.Split(',');
                if (spl.Length > 1)
                {
                    sb.Append(spl[1]);
                    if (spl[1].Last() == '!' || spl[1].Last() == '?' || spl[1].Last() == '.')
                        continue;
                    sb.Append(". ");
                }
            }

            List<IReadOnlyList<string>> sentences;
            sentences = SimpleTokeniser.FindSentences(SimpleTokeniser.Tokenise(sb.ToString()))
    .ToList();

            var sentencesRW = sentences
                .Select(m => m.ToList())
                .Where(m => m.Count > 1)
                .Where(m => !((m.Contains("https") || m.Contains("http")) && m.Count < 10))
                .Where(m => !(m[0] == "co"))
                .ToList();

            var trainer = BrightWireProvider.CreateMarkovTrainer3<string>();
            foreach (var sentence in sentencesRW)
                trainer.Add(sentence);
            var model = trainer.Build().AsDictionary;

            // generate some text
            for (var i = 0; i < 5000000; i++)
            {
                sb = new StringBuilder();
                string prevPrev = default(string), prev = default(string), curr = default(string);
                for (var j = 0; j < 256; j++)
                {
                    var transitions = model.GetTransitions(prevPrev, prev, curr);
                    var distribution = new Categorical(transitions.Select(d => Convert.ToDouble(d.Probability)).ToArray());
                    var next = transitions[distribution.Sample()].NextState;
                    if (Char.IsLetterOrDigit(next[0]) && sb.Length > 0)
                    {
                        var lastChar = sb[sb.Length - 1];
                        if (lastChar != '\'' && lastChar != '-')
                            sb.Append(' ');
                    }
                    sb.Append(next);

                    if (SimpleTokeniser.IsEndOfSentence(next))
                        break;
                    prevPrev = prev;
                    prev = curr;
                    curr = next;
                }

                if (sb.Length < 10)
                    continue;

                if (i % 10000 == 0)
                    Console.WriteLine($"Writing line {i}");

                File.AppendAllText("sts.txt", sb.ToString() + Environment.NewLine);
            }
        }
    }
}