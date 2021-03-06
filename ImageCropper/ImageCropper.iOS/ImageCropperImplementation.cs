﻿using System;
using System.Diagnostics;
using CoreGraphics;
using Foundation;
using Plugin.Media.Abstractions;
using TimOliver.TOCropViewController.Xamarin;
using UIKit;

namespace Xamarin.ImageCropper.iOS
{
    public class ImageCropperImplementation : IImageCropperWrapper
    {
        public void ShowFromFile(ImageCropper imageCropper, MediaFile imageFile)
        {
            try
            {
                UIImage image = UIImage.FromFile(imageFile.Path);

                TOCropViewController cropViewController;

                if (imageCropper.CropShape == ImageCropper.CropShapeType.Oval)
                {
                    cropViewController = new TOCropViewController(TOCropViewCroppingStyle.Circular, image);
                }
                else
                {
                    cropViewController = new TOCropViewController(image);
                }

                cropViewController.AspectRatioPreset = TOCropViewControllerAspectRatioPreset.Square;
                cropViewController.ResetAspectRatioEnabled = true;
                cropViewController.AspectRatioLockEnabled = true;
                cropViewController.Delegate = new ImageCroppingDelegate();

                if (imageCropper.AspectRatioX > 0 && imageCropper.AspectRatioY > 0)
                {
                    cropViewController.ResetAspectRatioEnabled = false;
                    cropViewController.AspectRatioLockEnabled = true;
                    cropViewController.AspectRatioPreset = TOCropViewControllerAspectRatioPreset.Custom;
                    cropViewController.CustomAspectRatio = new CGSize(imageCropper.AspectRatioX, imageCropper.AspectRatioY);
                }

                cropViewController.OnDidCropToRect = (outImage, cropRect, angle) =>
                {
                    outImage.CroppedImageWithFrame(cropRect, angle, false);
                    Finalize(imageCropper, outImage);
                };

                cropViewController.OnDidCropToCircleImage = (outImage, cropRect, angle) =>
                {
                    outImage.CroppedImageWithFrame(cropRect, angle, true);
                    Finalize(imageCropper, outImage);
                };

                cropViewController.OnDidFinishCancelled = (cancelled) =>
                {
                    imageCropper.Failure?.Invoke();
                    UIApplication.SharedApplication.KeyWindow.RootViewController.DismissViewController(true, null);
                };

                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(cropViewController, true, null);

                if (!string.IsNullOrWhiteSpace(imageCropper.PageTitle) && cropViewController.TitleLabel != null)
                {
                    UILabel titleLabel = cropViewController.TitleLabel;
                    titleLabel.Text = imageCropper.PageTitle;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static async void Finalize(ImageCropper imageCropper, UIImage image)
        {
            try
            {
                string documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string jpgFilename = System.IO.Path.Combine(documentsDirectory, Guid.NewGuid().ToString() + ".jpg");
                NSData imgData = image.AsJPEG();
                NSError err;

                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(100));
                if (imgData.Save(jpgFilename, false, out err))
                {
                    imageCropper.Success?.Invoke(jpgFilename);
                }
                else
                {
                    Debug.WriteLine("NOT saved as " + jpgFilename + " because" + err.LocalizedDescription);
                    imageCropper.Failure?.Invoke();
                }

                UIApplication.SharedApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}