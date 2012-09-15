/*

 */

namespace ubiety.net.dns.Records.NotUsed
{
	public class RecordSPF : Record
	{
		public byte[] RDATA;

		public RecordSPF(RecordReader rr)
		{
			// re-read length
			ushort RDLENGTH = rr.ReadUInt16(-2);
			RDATA = rr.ReadBytes(RDLENGTH);
		}

		public override string ToString()
		{
			return string.Format("not-used");
		}

	}
}
