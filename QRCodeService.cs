using QRCoder;
using System.Drawing;

public class QRCodeService
{
    public Bitmap GenerateQRCode(
        string text,
        QRCoder.QRCodeGenerator.ECCLevel eccLevel = QRCoder.QRCodeGenerator.ECCLevel.Q,
        int? qrVersion = null,
        Color? qrColor = null,
        Color? bgColor = null)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be empty");

        var qrGenerator = new QRCoder.QRCodeGenerator();
        QRCoder.QRCodeData qrCodeData;
        if (qrVersion.HasValue)
            qrCodeData = qrGenerator.CreateQrCode(text, eccLevel, forceUtf8: true, utf8BOM: false, QRCoder.QRCodeGenerator.EciMode.Default, requestedVersion: qrVersion.Value);
        else
            qrCodeData = qrGenerator.CreateQrCode(text, eccLevel, forceUtf8: true, utf8BOM: false, QRCoder.QRCodeGenerator.EciMode.Default);

        var qrCode = new QRCoder.QRCode(qrCodeData);
        return qrCode.GetGraphic(
            20,
            qrColor ?? Color.Black,
            bgColor ?? Color.White,
            true // drawQuietZones
        );
    }
}