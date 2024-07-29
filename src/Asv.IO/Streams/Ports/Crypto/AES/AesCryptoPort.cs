using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

public class AesCryptoPort : PortBase
{
    private readonly Aes _aes;
    private readonly PortBase _port;
    private CryptoStream _encryptorStream;
    private CryptoStream _decryptorStream;
    private MemoryStream _encryptMemStream;
    private MemoryStream _decryptMemStream;

    private static readonly byte[] HEADER = BitConverter.GetBytes(0xDEADBEEF);

    public AesCryptoPort(PortBase port, AesCryptoPortConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _port = port ?? throw new ArgumentNullException(nameof(port));
        
        _aes = Aes.Create();
        _aes.Key = config.Key;
        _aes.IV = config.InitVector;
        _aes.Padding = PaddingMode.PKCS7;

        ResetStreams();

        _port.Subscribe(OnNewData).DisposeItWith(Disposable);
    }

    private void ResetStreams()
    {
        _encryptorStream?.Dispose();
        _decryptorStream?.Dispose();
        _encryptMemStream?.Dispose();
        _decryptMemStream?.Dispose();

        _encryptMemStream = new MemoryStream();
        _decryptMemStream = new MemoryStream();
        
        _encryptorStream = new CryptoStream(_encryptMemStream, _aes.CreateEncryptor(), CryptoStreamMode.Write);
        _decryptorStream = new CryptoStream(_decryptMemStream, _aes.CreateDecryptor(), CryptoStreamMode.Write);
    }

    private void ResetEncryptor()
    {
        _encryptorStream?.Dispose();
        _encryptMemStream?.Dispose();
        _encryptMemStream = new MemoryStream();
        _encryptorStream = new CryptoStream(_encryptMemStream, _aes.CreateEncryptor(), CryptoStreamMode.Write);
    }
    
    private void ResetDecryptor()
    {
        _decryptorStream?.Dispose();
        _decryptMemStream?.Dispose();
        _decryptMemStream = new MemoryStream();
        _decryptorStream = new CryptoStream(_decryptMemStream, _aes.CreateDecryptor(), CryptoStreamMode.Write);
    }

    public override PortType PortType => _port.PortType;
    public override string PortLogName => $"AES {_port.PortLogName}";

    protected override async Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel)
    {
        try
        {
            ResetEncryptor();
            _encryptMemStream.SetLength(0);
            await _encryptMemStream.WriteAsync(HEADER, cancel);
            await _encryptorStream.WriteAsync(data, cancel);
            await _encryptorStream.FlushFinalBlockAsync(cancel);
            var encryptedData = _encryptMemStream.ToArray();
            await _port.Send(encryptedData, cancel);
        }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
    }

    protected override async Task InternalSend(byte[] data, int count, CancellationToken cancel)
    {
        try
        {
            ResetEncryptor();
            _encryptMemStream.SetLength(0);
            await _encryptMemStream.WriteAsync(HEADER, cancel);
            await _encryptorStream.WriteAsync(data.AsMemory(0, count), cancel);
            await _encryptorStream.FlushFinalBlockAsync(cancel);
            var encryptedData = _encryptMemStream.ToArray();
            await _port.Send(encryptedData, encryptedData.Length, cancel);
        }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
    }

    protected override void InternalStop()
    {
        _port.Disable();
        _encryptorStream.Dispose();
        _decryptorStream.Dispose();
        _encryptMemStream.Dispose();
        _decryptMemStream.Dispose();
    }

    protected override void InternalStart()
    {
        _port.Enable();
        ResetStreams(); 
    }

    private async void OnNewData(byte[] data)
    {
        if (IsEncrypted(data))
        {
            try
            {
                ResetDecryptor();
                _decryptMemStream.SetLength(0);
                await _decryptorStream.WriteAsync(data.AsMemory(HEADER.Length));
                await _decryptorStream.FlushFinalBlockAsync();
                var decryptedData = _decryptMemStream.ToArray();
                InternalOnData(decryptedData);
            }
            catch (CryptographicException)
            {
                InternalOnData(data);
            }
            catch (Exception ex)
            {
                InternalOnError(ex);
            }
        }
        else InternalOnData(data);
    }
    
    private static bool IsEncrypted(byte[] data)
    {
        if (data.Length < HEADER.Length) return false;
        return !HEADER.Where((t, i) => data[i] != t).Any();
    }
}
