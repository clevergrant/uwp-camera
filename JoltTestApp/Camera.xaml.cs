using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace JoltTestApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Camera : Page
	{
		//Create a mediacapture object for the camera, and a bool to show the preview
		MediaCapture mediaCapture;
		bool isPreviewing;

		//Create displayrequest for the preview
		DisplayRequest displayRequest = new DisplayRequest();

		public Camera()
		{
			InitializeComponent();

			//Handle closing the app while the preview is going
			Application.Current.Suspending += Application_Suspending;
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			//Initialize the mediacapture object on page load
			mediaCapture = new MediaCapture();
			await mediaCapture.InitializeAsync();

			//Handle a fail error
			mediaCapture.Failed += MediaCapture_Failed;

			//Start the preview on page load
			await StartPreviewAsync();
		}

		private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
		{
			throw new NotImplementedException();
		}

		private async void Capture_Click(object sender, RoutedEventArgs e)
		{
			// Prepare and capture photo
			var lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
			var capturedPhoto = await lowLagCapture.CaptureAsync();

			//Save captured photo to a software bitmap
			var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

			await lowLagCapture.FinishAsync();

			//open pictures library
			StorageFolder picturesFolder = KnownFolders.PicturesLibrary;

			//create a Jolt Pictures folder, or open it if it already exists
			var JoltFolder = await picturesFolder.CreateFolderAsync("Jolt Pictures", CreationCollisionOption.OpenIfExists);

			//save the image, and generate a new name if there's one already with the same name
			StorageFile file = await JoltFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

			//Create a temporary capture stream
			using (var captureStream = new InMemoryRandomAccessStream())
			{
				await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

				//Create a temporary file stream
				using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
				{
					//Create a jpeg from the bitmap
					var decoder = await BitmapDecoder.CreateAsync(captureStream);
					var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

					var properties = new BitmapPropertySet {
						{
							"System.Photo.Orientation",
							new BitmapTypedValue(PhotoOrientation.Normal,
							PropertyType.UInt16)
						}
					};
					await encoder.BitmapProperties.SetPropertiesAsync(properties);

					await encoder.FlushAsync();

					//Navigate back to the main page to display image
					Frame.Navigate(typeof(MainPage));
				}
			}
		}

		private async Task StartPreviewAsync()
		{
			try
			{
				//Open the image preview
				mediaCapture = new MediaCapture();
				await mediaCapture.InitializeAsync();

				displayRequest.RequestActive();
				DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
			}
			catch (UnauthorizedAccessException)
			{
				// This will be thrown if the user denied access to the camera in privacy settings
				ShowMessageToUser("The app was denied access to the camera");
				return;
			}

			try
			{
				//Set isPreviewing to true, and start the preview
				PreviewControl.Source = mediaCapture;
				await mediaCapture.StartPreviewAsync();
				isPreviewing = true;
			}
			catch (System.IO.FileLoadException)
			{
				mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
			}

		}

		private void ShowMessageToUser(string v)
		{
			throw new NotImplementedException();
		}

		private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
		{
			//Handle what happens if there's another app that has exclusive access to the camera
			if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
			{
				ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
			}
			else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
			{
				await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await StartPreviewAsync();
				});
			}
		}

		private async Task CleanupCameraAsync()
		{
			//Gets rid of any leftover memory
			if (mediaCapture != null)
			{
				if (isPreviewing)
				{
					await mediaCapture.StopPreviewAsync();
				}

				await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					PreviewControl.Source = null;
					if (displayRequest != null)
					{
						displayRequest.RequestRelease();
					}

					mediaCapture.Dispose();
					mediaCapture = null;
				});
			}

		}

		protected async override void OnNavigatedFrom(NavigationEventArgs e)
		{
			//if you navigate away from the page for any reason just run the camera cleanup
			await CleanupCameraAsync();
		}

		private async void Application_Suspending(object sender, SuspendingEventArgs e)
		{
			// Handle global application events only if this page is active
			if (Frame.CurrentSourcePageType == typeof(MainPage))
			{
				var deferral = e.SuspendingOperation.GetDeferral();
				await CleanupCameraAsync();
				deferral.Complete();
			}
		}
	}
}
