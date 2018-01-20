using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace JoltTestApp
{
	/// <summary>
	/// Main app page
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//Navigates to the Camera
			Frame.Navigate(typeof(Camera));
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			//When the app navigates here:

			//Find the Pictures folder for the current user
			StorageFolder picturesFolder = KnownFolders.PicturesLibrary;

			//Create a Jolt Pictures folder, or open it if it already exists
			StorageFolder JoltFolder = await picturesFolder.CreateFolderAsync("Jolt Pictures", CreationCollisionOption.OpenIfExists);

			//Get a list of the files
			IReadOnlyList<StorageFile> files = await JoltFolder.GetFilesAsync();

			foreach (StorageFile file in files)
			{

				//For each file, make a new grid Xaml object
				Grid obj = new Grid();

				//Create a new bitmap image for that file
				BitmapImage bmi = await StorageFileToBitmapImage(file);
				
				//Create a new image object, set the styles and the source
				Image img = new Image
				{
					Source = bmi,
					MaxWidth = 50,
					MaxHeight = 50,
					Margin = new Thickness {
						Top = 20
					}
				};
				
				//Create a button to delete the image
				Button delbtn = new Button
				{
					Content = "Delete",
					RequestedTheme = ElementTheme.Dark,
					Margin = new Thickness {
						Top = 20,
						Left = 130
					},
					//Store the name of the file as a string in the Tag of the object
					Tag = file.DisplayName.ToString()
				};

				//Add an event handler for the button
				delbtn.Click += Delete_Click;

				//Add the img object and the button object to the grid
				obj.Children.Add(img);
				obj.Children.Add(delbtn);

				//Add the grid to the stack panel object on the xaml page
				JoltImages.Children.Add(obj);
			}
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			//Create an object for the sender button
			Button clicked = (Button)sender;

			//Open the pictures library
			StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
			StorageFolder JoltFolder = await picturesFolder.CreateFolderAsync("Jolt Pictures", CreationCollisionOption.OpenIfExists);
			IReadOnlyList<StorageFile> files = await JoltFolder.GetFilesAsync();

			//Iterate through the files in the folder, then delete the one that matches the tag from the sender
			foreach (StorageFile file in files)
			{
				if (file.DisplayName.ToString() == clicked.Tag.ToString())
				{
					await file.DeleteAsync();
					//Reload the page to see the changes
					Frame.Navigate(typeof(MainPage));
				}
			}
		}

		public static async Task<BitmapImage> StorageFileToBitmapImage(StorageFile savedStorageFile)
		{
			//Create a temporary filestream
			using (IRandomAccessStream fileStream = await savedStorageFile.OpenAsync(FileAccessMode.Read))
			{
				//Create a new bitmap image to return
				BitmapImage bitmapImage = new BitmapImage
				{
					DecodePixelHeight = 150,
					DecodePixelWidth = 150
				};
				await bitmapImage.SetSourceAsync(fileStream);
				return bitmapImage;
			}
		}

	}
}
