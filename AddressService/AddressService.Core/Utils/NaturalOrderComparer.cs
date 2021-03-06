﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AddressService.Core.Utils
{
    public class NaturalOrderComparer : IComparer<string>
    {
        private static readonly Regex _numbers = new Regex("([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public int Compare(string a, string b)
        {
            if (a == b)
            {
                return 0;
            }
            else if (a == null)
            {
                return -1;
            }
            else if (b == null)
            {
                return 1;
            }

            string[] aSplit, bSplit;

            //splitting into sequences of numbers and characters
            aSplit = _numbers.Split(a);
            bSplit = _numbers.Split(b);

            //don't necessarily like the early returns but in this case it's helpful...
            for (int i = 0; i < aSplit.Length && i < bSplit.Length; i++)
            {
                //skip identical sequences
                if (aSplit[i] == bSplit[i])
                    continue;

                //if either sequences can't be parsed as an int, use a string comparison
                if (!int.TryParse(aSplit[i], out int aInt))
                    return aSplit[i].CompareTo(bSplit[i]);

                if (!int.TryParse(bSplit[i], out int bInt))
                    return aSplit[i].CompareTo(bSplit[i]);

                //both parsed as ints, use int comparison
                return
                    aInt.CompareTo(bInt);
            }

            //Rare scenario where one string is a perfect substring of another
            if (aSplit.Length > bSplit.Length)
                return -1;
            else if (aSplit.Length < bSplit.Length)
                return 1;
            else
                return 0;
        }

    }
}
