using DLIOutletController.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DLIOutletController
{
    public static class Extensions
    {
        public static async Task<string> GetStringSafeAsync(this HttpClient client, Uri uri)
        {
            var bytes = await client.GetByteArrayAsync(uri);

            //TODO: Decide on charset?
            var str = System.Text.Encoding.UTF8.GetString(bytes);

            return str;
        }

        public static async Task<string> GetStringSafeAsync(this HttpContent client)
        {
            var bytes = await client.ReadAsByteArrayAsync();

            var str = System.Text.Encoding.UTF8.GetString(bytes);

            return str;
        }

        public static byte[] ToByteArray(this string str)
        {
            return System.Text.ASCIIEncoding.ASCII.GetBytes(str);
        }


        public static string ToDetailsString(this OutletInfo outlet)
        {
            return $"{outlet.Index} - {outlet.IsOn} - {outlet.Name}";
        }


        public static string ToDetailsString(this SwitchInfo switchInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Switch: {switchInfo.Name}");
            foreach (var o in switchInfo.Outlets)
            {
                sb.AppendLine(o.ToDetailsString());
            }
            return sb.ToString();
        }
    }
}
