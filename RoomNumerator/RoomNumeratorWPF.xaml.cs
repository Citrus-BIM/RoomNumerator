using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RoomNumerator
{
    public partial class RoomNumeratorWPF : Window
    {
        public string NumberPrefix;
        public string StartFrom;
        public string SelectedNumberingDirection;

        public RoomNumeratorWPF()
        {
            InitializeComponent();
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            ApplyAndClose(true);
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ApplyAndClose(false);
        }

        private void RoomNumeratorWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyAndClose(true);
            }
            else if (e.Key == Key.Escape)
            {
                ApplyAndClose(false);
            }
        }

        private void ApplyAndClose(bool ok)
        {
            if (!ok)
            {
                DialogResult = false;
                Close();
                return;
            }

            NumberPrefix = textBox_NumberPrefix.Text;
            StartFrom = textBox_StartFrom.Text;

            RadioButton checkedRb = FindVisualChildren<RadioButton>(groupBox_NumberingDirection)
                .FirstOrDefault(rb => rb.IsChecked == true);

            SelectedNumberingDirection = checkedRb?.Name ?? "radioButton_RightAndDown";

            DialogResult = true;
            Close();
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            int count = VisualTreeHelper.GetChildrenCount(depObj);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}