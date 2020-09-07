using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class RsaHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRsa rsa = (Rsa)context["rsa"];
			/*
			string enc = rsa.EncryptToBase64("/home/fanxing/tmp/public.key", "My name is Fenkey.");
			string dec = rsa.DecryptFromBase64("/home/fanxing/tmp/private.key", enc);
			*/

			string publicKey = @"-----BEGIN RSA PUBLIC KEY-----
MIGJAoGBAMoUB2o6IpRYl5aXGB7ZIAUGMRgIr49+tml2NVVVdSXIA+cTCjhBgNEc
njtC2uUnmpoNFTIq24ec7E4QIh8LeFkjtnHkkKp83g0HmfzbaSGiyfGudr4mHXWL
1YbJVuRgpfx+XBXwdglEdQwjLXPbr4nFHp3iTGSErFEYeoJ12TY5AgMBAAE=
-----END RSA PUBLIC KEY-----";

			string privateKey = @"-----BEGIN RSA PRIVATE KEY-----
Proc-Type: 4,ENCRYPTED
DEK-Info: DES-EDE3-CBC,39143BA30783E8DB

5GWWXJxOj2FkuPmDnWe/sE8wEUr7M1DxbhBh/mtakzc07f/OK1TIMptg4IeJzI+/
jVjIWRlMGlzXtTGxUN5bDXQ6WdYrJEE8vh/O04F9ZE2aaMv0yZ2OBvg2N9K6fpCC
pDqTAnPtQe5DXpv3ptTJ9jojGvArkQODiaRBoDarjYIw0zd1FAmZxfMTjcmcwcQM
oDiHTRj38N5MN2Yydl951Wv2fAFZu3VVrW7XZVt1f+Oll1yn31JV5cW3zgcIubMN
AJ0jL3K99oqRFrBnKsquWoZnG+3jA+stPsYKWU/ngsFpnWxupYx+f+pIUqpkeLMX
tTvHiWD/qjTG+NYGYcjNpwCNUC+yCoX6kUTU2XRu6CWQW2yoqjbzb23JXi+Viq4z
LIFE5qJXuJaOf21wN+oQbc66N10GQja8AlZgXMKrdpzaJJYxFfxenvy6ke6skW4F
VLryIV0CyAWmrvqLESwdDNfP2sNwgCoCtzdLgxOxgdp1jS6eo/uRjx41lw5FqSV5
WmwKtHmSkNhCWYRgV2kDs+DOP0mtNv7xot+XkaGiufFuPUjwAV7k7NDEF5xmli3S
BDW6TmivP5962pA1va6Vbvnuu5lHMjyfNmGeBSkqqZLOdZg0KmbmYAyVKZ8eBimy
64aBM4HhMI22J9Y3wFR6G2ccPLWt6X3H4NB/PrLk2XBcemWD6eXa4c2t4azLJzOC
FT13t/1adRN5Ju35pBFsQwtX1eJ/GlaykrYDLc/18U4YmSg3EXqaoLWIfjD5bxAh
gi8cqDb1MXR+FkIACaCDUvjW3MrlZB2GZlIwjWZiZN6o//zXwm9LBQ==
-----END RSA PRIVATE KEY-----";

			string content = "My name is Fenkey.";

			string enc = rsa.MemEncryptToBase64(publicKey, content);
			string dec = rsa.MemDecryptFromBase64(privateKey, enc);

			string sign = rsa.MemSignToBase64(Algorithm.SHA1, privateKey, content);
			bool ok = rsa.MemVerifyFromBase64(Algorithm.SHA1, publicKey, content, sign);

			context.Response.Echo("plain text is: {0}<br/>verify result: {1}", dec, ok);

			Logger.DEBUG("Rsa handle success.");
		}
	}
}
