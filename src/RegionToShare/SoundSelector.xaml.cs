using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RegionToShare.Properties;

namespace RegionToShare;

/// <summary>
/// Selects a built in click sound, a wave file or no sound.
/// </summary>
public partial class SoundSelector
{
    private const string FileKey = "*file*";

    private static readonly KeyValuePair<string, string>[] Options =
    {
        new(ClickSounds.None, Properties.Resources.Sound_None),
        new(ClickSounds.High, Properties.Resources.Sound_High),
        new(ClickSounds.Low, Properties.Resources.Sound_Low),
        new(ClickSounds.Pop, Properties.Resources.Sound_Pop),
        new(FileKey, Properties.Resources.Sound_File),
    };

    private bool _isUpdating;

    public SoundSelector()
    {
        InitializeComponent();

        SoundComboBox.ItemsSource = Options;
        UpdateSelection();
    }

    /// <summary>
    /// The sound as stored in the settings: empty, a built in sound name, or the path of a wave file.
    /// </summary>
    public string? Sound
    {
        get => (string?)GetValue(SoundProperty);
        set => SetValue(SoundProperty, value);
    }
    public static readonly DependencyProperty SoundProperty = DependencyProperty.Register(nameof(Sound), typeof(string), typeof(SoundSelector),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((SoundSelector)d).UpdateSelection()));

    private void UpdateSelection()
    {
        _isUpdating = true;

        try
        {
            var sound = Sound ?? ClickSounds.None;
            var isFile = sound.Length > 0 && !ClickSounds.IsBuiltIn(sound);

            SoundComboBox.SelectedItem = Options.First(item => item.Key == (isFile ? FileKey : sound));
            FileNameText.Text = isFile ? Path.GetFileName(sound) : string.Empty;
            FileNameText.ToolTip = isFile ? sound : null;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || SoundComboBox.SelectedItem is not KeyValuePair<string, string> selected)
            return;

        if (selected.Key != FileKey)
        {
            Sound = selected.Key;
            return;
        }

        var dialog = new OpenFileDialog { Filter = Properties.Resources.Sound_FileFilter };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            Sound = dialog.FileName;
        }

        // Also reverts the selection if the dialog was canceled.
        UpdateSelection();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ClickSounds.Play(Sound, Settings.Default.HighlighterVolume);
    }
}
