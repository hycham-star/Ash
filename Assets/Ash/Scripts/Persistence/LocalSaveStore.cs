using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ash
{
    public enum ProgressLoadStatus { Missing, Primary, Backup, Invalid, Unsupported }
    public sealed class ProgressLoadResult
    {
        public ProgressLoadStatus Status { get; }
        public ProgressState State { get; }
        public string Detail { get; }
        public bool Loaded => Status==ProgressLoadStatus.Primary || Status==ProgressLoadStatus.Backup;
        internal ProgressLoadResult(ProgressLoadStatus status,ProgressState state,string detail) { Status=status; State=state; Detail=detail; }
    }

    /// <summary>Checksum-protected JSON payload, durable temp write, atomic promotion, previous-good backup.</summary>
    public sealed class LocalSaveStore
    {
        private const string Header="ASHLANDS_LOCAL_SAVE_1";
        private const int MaxBytes=4*1024*1024;
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        private readonly IProgressCodec codec;
        private readonly Action<ProgressState> validate;
        public string SavePath { get; }
        public string BackupPath => SavePath+".bak";
        public LocalSaveStore(string directory,IProgressCodec codec,Action<ProgressState> validate)
        {
            if(string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is required.");
            SavePath=Path.Combine(directory,"progress.save");
            this.codec=codec ?? throw new ArgumentNullException(nameof(codec));
            this.validate=validate ?? throw new ArgumentNullException(nameof(validate));
        }
        public ProgressLoadResult Load()
        {
            bool any=false; var errors=new StringBuilder();
            foreach(var path in new[] { SavePath,BackupPath })
            {
                if(!File.Exists(path)) continue; any=true;
                try { return new ProgressLoadResult(path==SavePath ? ProgressLoadStatus.Primary : ProgressLoadStatus.Backup,Read(path),errors.ToString()); }
                catch(UnsupportedSaveVersionException error) { return new ProgressLoadResult(ProgressLoadStatus.Unsupported,null,error.Message); }
                catch(Exception error) when(IsRecoverable(error)) { errors.Append(Path.GetFileName(path)).Append(": ").Append(error.Message).Append(' '); }
            }
            return new ProgressLoadResult(any ? ProgressLoadStatus.Invalid : ProgressLoadStatus.Missing,null,errors.ToString());
        }
        public void Save(ProgressState state) { Write(state,false); }
        public void Reset(ProgressState freshState) { Write(freshState,true); }
        private void Write(ProgressState state,bool reset)
        {
            validate(state);
            string json=codec.Encode(state) ?? throw new InvalidDataException("Codec produced no progress data.");
            // Ensure the codec itself can round-trip a complete valid state before touching disk.
            validate(codec.Decode(json));
            string encoded=Header+"\n"+Digest(json)+"\n"+json;
            byte[] bytes=Utf8.GetBytes(encoded);
            if(bytes.Length>MaxBytes) throw new InvalidDataException("Save is too large.");
            bool goodPrimary=false;
            if(!reset)
            {
                foreach(var path in new[] { SavePath,BackupPath })
                {
                    if(!File.Exists(path))continue;
                    try { Read(path); if(path==SavePath)goodPrimary=true; }
                    catch(UnsupportedSaveVersionException) { throw; } // Never overwrite a future-version save.
                    catch(Exception error) when(IsRecoverable(error)) { /* Keep a known-good backup when primary is corrupt. */ }
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporary=SavePath+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
                { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
                // Remove old backup only for an explicit reset, after the new payload is durable.
                // If reset promotion fails, the existing primary remains intact.
                if(reset && File.Exists(BackupPath))File.Delete(BackupPath);
                if(File.Exists(SavePath))File.Replace(temporary,SavePath,!reset && goodPrimary ? BackupPath : null);
                else File.Move(temporary,SavePath);
            }
            finally
            {
                try { if(File.Exists(temporary))File.Delete(temporary); } catch(IOException) { } catch(UnauthorizedAccessException) { }
            }
        }
        private ProgressState Read(string path)
        {
            var info=new FileInfo(path); if(info.Length>MaxBytes)throw new InvalidDataException("Save is too large.");
            string text=Utf8.GetString(File.ReadAllBytes(path));
            int first=text.IndexOf('\n'); int second=first<0 ? -1 : text.IndexOf('\n',first+1);
            if(first<0 || second<0)throw new InvalidDataException("Invalid save envelope.");
            string header=text.Substring(0,first);
            const string prefix="ASHLANDS_LOCAL_SAVE_";
            if(header.StartsWith(prefix,StringComparison.Ordinal) && int.TryParse(header.Substring(prefix.Length),out int version) && version>1)
                throw new UnsupportedSaveVersionException(version);
            if(header!=Header)throw new InvalidDataException("Invalid save envelope.");
            string hash=text.Substring(first+1,second-first-1), json=text.Substring(second+1);
            if(!string.Equals(hash,Digest(json),StringComparison.Ordinal))throw new InvalidDataException("Save checksum mismatch.");
            var state=codec.Decode(json); validate(state); return state;
        }
        private static string Digest(string json)
        {
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(json))).Replace("-","");
        }
        private static bool IsRecoverable(Exception error) => error is InvalidDataException || error is IOException || error is UnauthorizedAccessException
            || error is ArgumentException || error is FormatException || error is OverflowException || error is DecoderFallbackException;
    }
}
