using DustyPig.API.v3.Models;
using DustyPig.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class MediaEntryLogic
    {
        class NormHash
        {
            public NormHash() { }

            public NormHash(string norm, string hash, int order)
            {
                Norm = norm;
                Hash = hash;
                Order = order;
            }

            public string Norm { get; set; }
            public string Hash { get; set; }
            public int Order { get; set; }
        }


        private static List<string> FixList(List<string> lst)
        {
            if (lst == null)
                return new List<string>();

            return lst.Select(item => (item + string.Empty).NormalizeMiscCharacters().Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Substring(0, Math.Min(item.Length, Constants.MAX_NAME_LENGTH)))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Call FixList before passing to this method
        /// </summary>
        private static List<NormHash> CreateNormalizedList(List<string> lst, bool lowerCase)
        {
            var ret = new List<NormHash>();

            for (int i = 0; i < lst.Count; i++)
            {
                string norm = lst[i];
                if (lowerCase)
                    norm = norm.ToLower();

                string hash = Crypto.NormalizedHash(norm);
                if (!ret.Any(item => item.Hash == hash))
                    ret.Add(new NormHash(norm, hash, i));
            }

            return ret;
        }


    }
}