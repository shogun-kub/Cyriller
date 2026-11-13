using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Cyriller.Desktop.Views
{
    public static class ViewExtensions
    {
        public static Window GetParentWindow(this Control control)
        {
            Control current = control;

            while (current != null)
            {
                if (current is Window window)
                {
                    return window;
                }

                current = current.Parent as Control;
            }

            throw new InvalidOperationException("Current Control is not attached to any Window object.");
        }

        public static async Task<string> SaveFileDialog(this Control control, string title, string fileExtension, string fileTypeDescription)
        {
            Window window = control.GetParentWindow();

            if (window?.StorageProvider == null)
            {
                return null;
            }

            FilePickerFileType fileType = new FilePickerFileType(fileTypeDescription)
            {
                Patterns = new List<string> { "*." + fileExtension }
            };

            IStorageFile file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = fileExtension,
                FileTypeChoices = new List<FilePickerFileType> { fileType }
            });

            return file?.TryGetLocalPath();
        }
    }
}
