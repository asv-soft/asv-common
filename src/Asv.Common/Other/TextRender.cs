using System;
using System.Text;

namespace Asv.Common
{
    public class TextRender
    {
        /// <summary>
        /// Example: ██████░░░░░░ 50%")]
        /// </summary>
        /// <param name="value">Must be from 0.0 (0 %) to 1.0 (100%)</param>
        /// <param name="width">Width in char</param>
        /// <param name="fill">empty char</param>
        /// <param name="empty">fill char</param>
        /// <returns></returns>
        public static string Progress(double value, int width, string fill, string empty)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);
            const int labelWidth = 4;
            const int minWidth = labelWidth + 2;
            if (width < minWidth)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(width, minWidth);
            }

            var realWidth = width - labelWidth;
            var w1 = (int)(value * realWidth);
            var w2 = realWidth - w1;
            var sb = new StringBuilder();
            for (var i = 0; i < w1; i++)
            {
                sb.Append(fill);
            }

            for (var i = 0; i < w2; i++)
            {
                sb.Append(empty);
            }

            sb.Append(((int)(value * 100) + "%").PadLeft(labelWidth));
            return sb.ToString();
        }

        /// <summary>
        /// Example: ██████░░░░░░ 50%")].
        /// </summary>
        /// <param name="value">Must be from 0.0 (0 %) to 1.0 (100%).</param>
        /// <param name="width">Width in char.</param>
        /// <returns></returns>
        public static string Progress(double value, int width)
        {
            return Progress(value, width, "█", "░");
        }
    }
}
