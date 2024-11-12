// MainPage.xaml.cs
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;

namespace splitGZDoom
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<string> ResourceFiles { get; set; }
        public ICommand RemoveResourceCommand { get; }

        private const string HostPathKey = "HostPath";
        private const string ClientPathKey = "ClientPath";
        private const string IwadPathKey = "IwadPath";
        private const string WadPathsKey = "WadPaths";

        public MainPage()
        {
            InitializeComponent();
            ResourceFiles = new ObservableCollection<string>();
            BindingContext = this;
 


            DifficultyPicker.ItemsSource = new List<string>
            {
                "I'm Too Young To Die",
                "Hey, Not Too Rough",
                "Hurt Me Plenty",
                "Ultra-Violence",
                "Nightmare!"
            };
            DifficultyPicker.SelectedIndex = 2;

       

            // Initialize the RemoveResourceCommand with the correct logic
            RemoveResourceCommand = new Command<string>(file =>
            {
                Console.WriteLine("deleting");
                try
                {
                    ResourceFiles.Remove(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing file: {ex.Message}");
                }
            });

            // Initialize other components and load saved paths
            LoadSavedPaths();
        }
  
        private async void OnAddResourceClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                
                PickerTitle = "Select WAD/PK3 Files",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".wad", ".pk3" } }
            })
            });

            if (result != null)
            {
                foreach (FileResult filePath in result)
                {
                    if (!ResourceFiles.Contains(filePath.FullPath))
                    {
                        ResourceFiles.Add(filePath.FullPath);
                    }
                }
                
                
            }

        }

        private void LoadSavedPaths()
        {
            // Retrieve the saved paths or leave empty if not set
            HostPathEntry.Text = Preferences.Get(HostPathKey, string.Empty);
            ClientPathEntry.Text = Preferences.Get(ClientPathKey, string.Empty);
            IwadPathEntry.Text = Preferences.Get(IwadPathKey, string.Empty);
        }

        private void SavePaths()
        {
            Preferences.Set(HostPathKey, HostPathEntry.Text);
            Preferences.Set(ClientPathKey, ClientPathEntry.Text);
            Preferences.Set(IwadPathKey, IwadPathEntry.Text);
        }

        private async void OnBrowseHostPathClicked(object sender, EventArgs e)
        {
            FileResult? fileResult = await FilePicker.Default.PickAsync();
            if (fileResult != null)
            {
                HostPathEntry.Text = fileResult.FullPath;
                SavePaths();
            }
        }

        private async void OnBrowseClientPathClicked(object sender, EventArgs e)
        {
            var fileResult = await FilePicker.Default.PickAsync();
            if (fileResult != null)
            {
                ClientPathEntry.Text = fileResult.FullPath;
                SavePaths();
            }
        }

        private async void OnBrowseIwadPathClicked(object sender, EventArgs e)
        {
            FileResult? fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select IWAD File"
            });

            if (fileResult != null)
            {
                IwadPathEntry.Text = fileResult.FullPath;
                SavePaths();
            }
        }

        private void OnLaunchClicked(object sender, EventArgs e)
        {
            string levelWarp = LevelWarpEntry.Text; // Captur
            string gzdoomHost = HostPathEntry.Text;
            string gzdoomClient = ClientPathEntry.Text;
            string iwadFile = IwadPathEntry.Text;
            string? difficulty = DifficultyPicker.SelectedItem as string;

            if (string.IsNullOrEmpty(gzdoomHost) || string.IsNullOrEmpty(gzdoomClient) || string.IsNullOrEmpty(iwadFile))
            {
                DisplayAlert("Error", "Please specify all required paths.", "OK");
                return;
            }

            var difficultyMap = new Dictionary<string, string>
            {
                { "I'm Too Young To Die", "1" },
                { "Hey, Not Too Rough", "2" },
                { "Hurt Me Plenty", "3" },
                { "Ultra-Violence", "4" },
                { "Nightmare!", "5" }
            };
            string skill = difficultyMap.TryGetValue(difficulty, out string? value) ? value : "3";

            // Use ResourceFiles directly to build the arguments
            string resourceArguments = string.Join(" ", ResourceFiles.Select(w => $"\"{w}\""));
            var hostArguments = $"{resourceArguments} -iwad \"{iwadFile}\" -host 2 -skill {skill}";
            var clientArguments = $"{resourceArguments} -iwad \"{iwadFile}\" -join localhost";
            // If the user entered a valid level number, add the -warp argument
            if (int.TryParse(levelWarp, out int levelNumber))
            {
                hostArguments += $" -warp {levelNumber}";
            }
            DebugLogLabel.Text = $"Host Command: {gzdoomHost} {hostArguments}\nClient Command: {gzdoomClient} {clientArguments}";

            Process.Start(new ProcessStartInfo
            {
                FileName = gzdoomHost,
                Arguments = hostArguments,
                UseShellExecute = true
            });

            Task.Delay(2000).Wait();

            Process.Start(new ProcessStartInfo
            {
                FileName = gzdoomClient,
                Arguments = clientArguments,
                UseShellExecute = true
            });
        }
    }
}
