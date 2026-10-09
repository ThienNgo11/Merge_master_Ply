using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DatdevUlts.DateTimeScripts.Editor
{
    [CustomPropertyDrawer(typeof(DateTimeAsTicks))]
    public class DateTimeAsTicksDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var pos = EditorGUI.PrefixLabel(position, label);

            var att = (DateTimeAsTicks)attribute;

            string show;
            show = new DateTime(property.longValue).ToString(CultureInfo.InvariantCulture);
            
            var value = EditorGUI.TextField(pos, show);

            try
            {
                property.longValue = DateTime.Parse(value, CultureInfo.InvariantCulture).Ticks;
            }
            catch (Exception)
            {
                // ignored
            }

            EditorGUI.EndProperty();
        }
    }
    
    
    [CustomPropertyDrawer(typeof(TimeSpanAsTicks))]
    public class TimeSpanAsTicksDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var pos = EditorGUI.PrefixLabel(position, label);

            var att = (TimeSpanAsTicks)attribute;

            // Chuyển đổi ticks sang TimeSpan
            var timeSpan = TimeSpan.FromTicks(property.longValue);

            // Hiển thị TimeSpan theo định dạng năm:tháng:ngày giờ:phút:giây:miligiay
            string show = $"{(int)timeSpan.TotalDays / 365}:{(int)timeSpan.TotalDays % 365 / 30}:{(int)timeSpan.TotalDays % 30} " +
                          $"{timeSpan.Hours}:{timeSpan.Minutes}:{timeSpan.Seconds}:{timeSpan.Milliseconds}";

            // Nhận giá trị từ người dùng
            var value = EditorGUI.TextField(pos, show);

            try
            {
                // Phân tích giá trị nhập vào và cập nhật property
                var parts = value.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 7)
                {
                    int years = int.Parse(parts[0]);
                    int months = int.Parse(parts[1]);
                    int days = int.Parse(parts[2]);
                    int hours = int.Parse(parts[3]);
                    int minutes = int.Parse(parts[4]);
                    int seconds = int.Parse(parts[5]);
                    int milliseconds = int.Parse(parts[6]);

                    var newTimeSpan = new TimeSpan(days + months * 30 + years * 365, hours, minutes, seconds, milliseconds);
                    property.longValue = newTimeSpan.Ticks;
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi nếu không thể phân tích
            }

            EditorGUI.EndProperty();
        }
    }

}