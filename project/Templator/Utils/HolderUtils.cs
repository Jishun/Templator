﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;

namespace Templator
{
    public static class HolderUtils
    {
        public static object DecimalToString(this object value)
        {
            var ret = value.ParseDecimalNullable();
            if (ret.HasValue)
            {
                return ret.Value.ToStringTrim();
            }
            return value;
        }

        public static bool IsCollection(this TextHolder holder)
        {
            return holder.Children != null;
        }

        public static bool IsNullOrEmptyValue(this object value)
        {
            return value == null || Convert.ToString(value) == String.Empty;
        }
        public static bool ContainsKey(this TextHolder holder, string key)
        {
            return holder != null && holder.Params != null && holder.Params.ContainsKey(key);
        }

        public static int? ParseIntParam(this string src, int? defaultValue = null, bool throwIfFail = true)
        {
            var ret = 0;
            if (!int.TryParse(src, out ret))
            {
                if (throwIfFail && defaultValue == null)
                {
                    throw new TemplatorParamsException();
                }
                return defaultValue;
            }
            return ret;
        }
        public static decimal? ParseNumberParam(this string src, decimal? defaultValue = null, bool throwIfFail = true)
        {
            var ret = 0m;
            if (!decimal.TryParse(src, out ret))
            {
                if (throwIfFail)
                {
                    throw new TemplatorParamsException();
                }
                return null;
            }
            return ret;
        }

        public static bool IsOptional(this TextHolder holder)
        {
            return holder.Keywords.Any(k => k.IndicatesOptional);
        }

        public static TextHolder Clone(this TextHolder holder)
        {
            var ret = new TextHolder(holder.Name)
            {
                Category = holder.Category,
                Keywords = holder.Keywords.Select(k => k.Create()).ToList(),
                Params = holder.Params.Copy()
            };
            if (holder.Children != null)
            {
                ret.Children = holder.Children.ToDictionary(c => c.Key, c => c.Value.Clone());
            }
            return ret;
        }
    }
}