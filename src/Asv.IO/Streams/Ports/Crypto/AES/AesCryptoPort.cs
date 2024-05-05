using System;
using System.Diagnostics;
using System.IO;
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

    public override PortType PortType => _port.PortType;
    public override string PortLogName => $"AES {_port.PortLogName}";
    protected override async Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel)
    {
        var encryptedData = data;
        try
        {
            _encryptMemStream.SetLength(0);
            _encryptorStream = new CryptoStream(_encryptMemStream, _aes.CreateEncryptor(), CryptoStreamMode.Write); // TODO: why we create new stream every time ???
            await _encryptorStream.WriteAsync(data, cancel);
            await _encryptorStream.FlushFinalBlockAsync(cancel);
            encryptedData = _encryptMemStream.ToArray(); // TODO: memory allocation
        }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
        finally
        {
            await _port.Send(encryptedData, cancel); // TODO: send original data when error ??? WTF
        }
    }

    protected override async Task InternalSend(byte[] data, int count, CancellationToken cancel)
    {
        var encryptedData = data;
        try
        {
            _encryptMemStream.SetLength(0);
            _encryptorStream = new CryptoStream(_encryptMemStream, _aes.CreateEncryptor(), CryptoStreamMode.Write); // TODO: why we create new stream every time ???
            await _encryptorStream.WriteAsync(data.AsMemory(0, count), cancel);
            await _encryptorStream.FlushFinalBlockAsync(cancel);
            encryptedData = _encryptMemStream.ToArray();
        }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
        finally
        {
            await _port.Send(encryptedData, encryptedData.Length, cancel);
        }
    }

    protected override void InternalStop()
    {
        _port.Disable();
        _encryptorStream.Close();
        _decryptorStream.Close();
        _encryptMemStream.Close();
        _decryptMemStream.Close();
    }

    protected override void InternalStart()
    {
        _port.Enable();
        ResetStreams(); 
    }

    private async void OnNewData(byte[] data)
    {
        var decryptedData = data;
        try
        {
            _decryptMemStream.SetLength(0);
            _decryptorStream = new CryptoStream(_decryptMemStream, _aes.CreateDecryptor(), CryptoStreamMode.Write);
            await _decryptorStream.WriteAsync(data);
            await _decryptorStream.FlushFinalBlockAsync();
            decryptedData = _decryptMemStream.ToArray();
        }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
        finally
        {
            InternalOnData(decryptedData);
        }
    }
}
