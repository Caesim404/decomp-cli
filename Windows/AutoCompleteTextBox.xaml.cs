﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Decomp.Windows
{
    public partial class AutoCompleteTextBox
    {
        // ReSharper disable InconsistentNaming
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WIN32_FIND_DATA
        {
            public readonly FileAttributes dwFileAttributes;
            private readonly FILETIME ftCreationTime;
            private readonly FILETIME ftLastAccessTime;
            private readonly FILETIME ftLastWriteTime;
            private readonly uint nFileSizeHigh;
            private readonly uint nFileSizeLow;
            private readonly uint dwReserved0;
            private readonly uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public readonly string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            private readonly string cAlternateFileName;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);
        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);
        private const short INVALID_HANDLE_VALUE = -1;

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        // ReSharper restore InconsistentNaming

        private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr wParam, IntPtr lParam, ref bool bHandled)
        {
            switch (iMsg)
            {
                case WM_NCRBUTTONDOWN:
                case WM_NCLBUTTONDOWN:
                    Popup.IsOpen = false;
                    ItemsListBox.ItemsSource = null;
                    break;
                case WM_RBUTTONUP:
                case WM_LBUTTONUP:
                    Popup.IsOpen = false;
                    if (ItemsListBox.ItemsSource != null && ItemsListBox.SelectedIndex != -1)
                    {
                        SetText(ItemsListBox.SelectedItem.ToString());
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public IEnumerable<string> GetItems(string textPattern)
        {
            if (textPattern.Length < 2 || textPattern[1] != ':')
            {
                yield break;
            }
            int lastSlashPos = textPattern.LastIndexOf('\\');
            if (lastSlashPos == -1)
            {
                yield break;
            }
            int fileNamePatternLength = textPattern.Length - lastSlashPos - 1;
            string baseFolder = textPattern.Substring(0, lastSlashPos + 1);
            WIN32_FIND_DATA fd;
            var hFind = FindFirstFile(textPattern + "*", out fd);
            if (hFind.ToInt32() == INVALID_HANDLE_VALUE)
            {
                yield break;
            }
            do
            {
                if (fd.cFileName[0] == '.')
                {
                    continue;
                }
                if ((fd.dwFileAttributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }
                if (fileNamePatternLength > fd.cFileName.Length)
                {
                    continue;
                }
                yield return baseFolder + fd.cFileName;
            } while (FindNextFile(hFind, out fd));
            FindClose(hFind);
        }

        private bool _loaded;
        private bool _prevState;
        private ListBox ItemsListBox => Template.FindName("PART_ItemList", this) as ListBox;
        private Popup Popup => Template.FindName("PART_Popup", this) as Popup;
        private Grid Root => Template.FindName("root", this) as Grid;

        public AutoCompleteTextBox()
        {
            InitializeComponent();
        }

        private Window GetParentWindow()
        {
            DependencyObject d = this;
            while (d != null && !(d is Window))
                d = LogicalTreeHelper.GetParent(d);
            return d as Window;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _loaded = true;

            KeyDown += AutoCompleteTextBoxKeyDown;
            KeyUp += AutoCompleteTextBoxKeyUp;
            PreviewKeyDown += AutoCompleteTextBoxPreviewKeyDown;
            ItemsListBox.PreviewMouseDown += ItemsListBoxPreviewMouseDown;
            ItemsListBox.KeyDown += ItemsListBoxKeyDown;
            Popup.CustomPopupPlacementCallback += Repositioning;

            var parentWindow = GetParentWindow();
            if (parentWindow == null) return;
            parentWindow.Deactivated += (s, e) => { _prevState = Popup.IsOpen; Popup.IsOpen = false; };
            parentWindow.Activated += (s, e) => Popup.IsOpen = _prevState;

            var source = PresentationSource.FromVisual(parentWindow) as HwndSource;
            source?.AddHook(WndProc);
        }

        private CustomPopupPlacement[] Repositioning(Size popupSize, Size targetSize, Point offset)
        {
            return new[] { new CustomPopupPlacement(new Point((0.01 - offset.X), (Root.ActualHeight - offset.Y)), PopupPrimaryAxis.None) };
        }

        private bool _setText;
        public void SetText(string text)
        {
            _setText = true;
            base.Text = text;
            _setText = false;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!_loaded || _setText) return;
            base.OnTextChanged(e);
            try
            {
                var aVariants = GetItems(Text).ToList();
                ItemsListBox.ItemsSource = aVariants;
                Popup.IsOpen = ItemsListBox.Items.Count > 0;
            }
            catch { }
        }

        private void AutoCompleteTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            Popup.IsOpen = false;
            UpdateSource();
        }

        private void AutoCompleteTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Popup.IsOpen) Popup.IsOpen = false; 
        }

        private void AutoCompleteTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ItemsListBox.Items.Count <= 0 || e.OriginalSource is ListBoxItem) return;
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Prior:
                case Key.Next:
                    ItemsListBox.Focus();
                    ItemsListBox.SelectedIndex = 0;
                    var lbi = (ListBoxItem)ItemsListBox.ItemContainerGenerator.ContainerFromIndex(ItemsListBox.SelectedIndex);
                    lbi.Focus();
                    e.Handled = true;
                    break;

            }
        }

        private void ItemsListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.OriginalSource is ListBoxItem)) return;
            var tb = (ListBoxItem)e.OriginalSource;

            e.Handled = true;
            switch (e.Key)
            {
                case Key.Enter:
                    Text = tb.Content as string;
                    UpdateSource();
                    break;
                case Key.Oem5:
                    Text = (tb.Content as string) + "\\";
                    break;
                default:
                    e.Handled = false;
                    break;
            }
            if (!e.Handled) return;

            Keyboard.Focus(this);
            Popup.IsOpen = false;
            if (Text != null) Select(Text.Length, 0);
        }

        void ItemsListBoxPreviewMouseDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var tb = e.OriginalSource as TextBlock;
                if (tb != null)
                {
                    Text = tb.Text;
                    Select(Text.Length, 0);
                    UpdateSource();
                    Popup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }

        private void UpdateSource()
        {
            if (GetBindingExpression(TextProperty) == null) return;
            var bindingExpression = GetBindingExpression(TextProperty);
            bindingExpression?.UpdateSource();
        }
        
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            var text = e.Data.GetData(DataFormats.FileDrop);
            var strings = (string[])text;
            if (strings != null) SetText($"{strings[0]}");
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        public new void Paste()
        {
            _setText = true;
            base.Paste();
            _setText = false;
        }
        
        public new string Text { get { return base.Text; } set { SetText(value); } }
    }
}
