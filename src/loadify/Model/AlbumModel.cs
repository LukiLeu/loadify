using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using loadify.Spotify;
using SpotifySharp;
using Image = SpotifySharp.Image;

namespace loadify.Model
{
    public class AlbumModel
    {
        private readonly Album _UnmanagedAlbum;

        public string Name { get; set; }
        public int ReleaseYear { get; set; }
        public AlbumType AlbumType { get; set; }
        public byte[] Cover { get; set; }

        public AlbumModel(Album unmanagedAlbum)
        {
            _UnmanagedAlbum = unmanagedAlbum;
        }

        public AlbumModel()
        { }

        public static async Task<AlbumModel> FromLibrary(Album unmanagedAlbum, LoadifySession session)
        {
            var albumModel = new AlbumModel(unmanagedAlbum);
            if (unmanagedAlbum == null) return albumModel;
            await SpotifyObject.WaitForInitialization(unmanagedAlbum.IsLoaded);

            albumModel.Name = unmanagedAlbum.Name();
            albumModel.ReleaseYear = unmanagedAlbum.Year();
            albumModel.AlbumType = unmanagedAlbum.Type();

            try
            {
                // retrieve the cover image of the album...
                var coverImage = session.GetImage(unmanagedAlbum.Cover(ImageSize.Large));
                await SpotifyObject.WaitForInitialization(coverImage.IsLoaded);

                using (var input = new MemoryStream(coverImage.Data()))
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(input))
                    {
                        using (var output = new MemoryStream())
                        {
                            // Define Variable for the compressing of the cover
                            Encoder myEncoder = Encoder.Quality;
                            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 25L);
                            EncoderParameters myEncoderParameters = new EncoderParameters(1);
                            ImageCodecInfo myImageCodecInfo = GetEncoderInfo("image/jpeg");
                            myEncoderParameters.Param[0] = myEncoderParameter;

                            //Save compressed image
                            img.Save(output, myImageCodecInfo, myEncoderParameters);
                            albumModel.Cover = output.ToArray();
                        }
                    }
                }

            }
            catch (AccessViolationException)
            {
                // nasty work-around - swallow if the cover image could not be retrieved
                // since the ImageId class does not expose a property or function for checking if the buffer/handle is null/0
            }
            
            return albumModel;
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i <= encoders.Length; i++)
            {
                if (encoders[i].MimeType == mimeType) return encoders[i];
            }
            return null;
        }
    }
}
