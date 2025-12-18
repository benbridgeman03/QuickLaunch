using QuickLaunch.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickLaunch.UI.Converters
{
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemType type)
            {
                return type switch
                {
                    // Folder (Standard)
                    ItemType.Directory => "\uE8B7",

                    ItemType.Exe => "\uE74C",
                    ItemType.Shortcut => "\uE74C",
                    ItemType.UWP => "\uE74C",

                    _ => "\uE8A5"
                };
            }
            return "\uE8A5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
