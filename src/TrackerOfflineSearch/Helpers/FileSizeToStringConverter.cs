using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;

namespace TrackerOfflineSearch.Helpers;

public class FileSizeToStringConverter : IValueConverter
{
    private static readonly string[] Suffixes = { " B", " KB", " MB", " GB", " TB", " PB", " EB" }; //Longs run out around EB

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long byteCount = (value is long l) ? l : 0;

        return StrFormatByteSize(byteCount);

        //if (byteCount == 0)
        //    return "0" + Suffixes[0];

        //long bytes = Math.Abs(byteCount);
        //int place = System.Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        //double num = Math.Round(bytes / Math.Pow(1024, place), 2);
        //return (Math.Sign(byteCount) * num).ToString() + Suffixes[place];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
    public static extern long StrFormatByteSize(
            long fileSize
            , [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer
            , int bufferSize);


    /// <summary>
    /// Converts a numeric value into a string that represents the number expressed as a size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
    /// </summary>
    /// <param name="filelength">The numeric value to be converted.</param>
    /// <returns>the converted string</returns>
    public static string StrFormatByteSize(long filesize)
    {
        StringBuilder sb = new StringBuilder(11);
        StrFormatByteSize(filesize, sb, sb.Capacity);
        return sb.ToString();
    }

}
