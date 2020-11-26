#include <ctype.h>
#include <string.h>
#include <stdlib.h>
#include <assert.h>
#include <time.h>
#include <zlib.h>
#include <sys/socket.h>
#include <openssl/md5.h>
#include <openssl/sha.h>
#include <openssl/err.h>
#include "wrapper.h"
#include "misc.h"

void thread_sleep(int interval)
{
	struct timeval timeout = { .tv_sec = interval, .tv_usec = 0 };
	select(0, NULL, NULL, NULL, &timeout);
}

double second_diff(const struct timeval *tb, const struct timeval *te)
{
	double beg_time = tb->tv_sec + (double)tb->tv_usec / 1000000;
	double end_time = te->tv_sec + (double)te->tv_usec / 1000000;
	return end_time - beg_time;
}

int today()
{
	char buf[10];
	struct tm tm;
	time_t t = time(NULL);
	localtime_r(&t, &tm);
	strftime(buf, sizeof(buf), "%Y%m%d", &tm);
	return atoi(buf);
}

time_t now(int *v)
{
	time_t t = time(NULL);
	if (v) {
		struct tm tm;
		localtime_r(&t, &tm);
		v[0] = tm.tm_year + 1900;
		v[1] = tm.tm_mon + 1;
		v[2] = tm.tm_mday;
		v[3] = tm.tm_hour;
		v[4] = tm.tm_min;
		v[5] = tm.tm_sec;
	}
	return t;
}

time_t sometime(int y, int m, int d, int h, int M, int s)
{
	struct tm tm;
	memset(&tm, 0, sizeof(struct tm));
	tm.tm_year = y - 1900;
	tm.tm_mon = m - 1;
	tm.tm_mday = d;
	tm.tm_hour = h;
	tm.tm_min = M;
	tm.tm_sec = s;
	return mktime(&tm);
}

time_t oneday(int y, int m, int d)
{
	return sometime(y, m, d, 0, 0, 0);
}

char* time2string(time_t *time, char ymd[20])
{
	if (*time > 0) {
		struct tm tm;
		localtime_r(time, &tm);
		sprintf(ymd, "%04d-%02d-%02d %02d:%02d:%02d", \
		tm.tm_year+1900, tm.tm_mon+1, tm.tm_mday, tm.tm_hour, tm.tm_min, tm.tm_sec);
	} else
		strcpy(ymd, "null");
	return ymd;
}

const char* memstr(const char *buf, int blen, const char *str, int slen)
{
	if (blen < slen)
		return NULL;
	const char *p = buf;
	for (;;) {
		const char *q = memchr(p, (int)*str, blen - (p - buf));
		if (!q)
			break;
		if (blen - (q - buf) < slen)
			break;
		if (memcmp(q, str, slen) == 0)
			return q;
		p = q + 1;
	}
	return NULL;
}

const char* memcasestr(const char *buf, int blen, const char *str, int slen)
{
	if (blen < slen)
		return NULL;
	const char *p = buf;
	for (;;) {
		const char *q = memchr(p, (int)*str, blen - (p - buf));
		if (!q) {
			q = memchr(p, tolower(*str), blen - (p - buf));
			if (!q)
				break;
		}
		if (blen - (q - buf) < slen)
			break;
		const char *_q = q;
		while (_q - q <= slen) {
			if (tolower(*_q) != tolower(*(str + (_q - q))))
				break;
			_q++;
		}
		if (_q - q == slen)
			return q;
		p = q + 1;
	}
	return NULL;
}

/*
 * Refer to the warning about strncpy:
 * "Warning: If there is no null byte among the first n bytes of src,
 * the string placed in dest will not be null-terminated."
 */
char* sstrncpy(char *dest, const char *src, int n)
{
	if (!dest || !src || n < 1)
		return NULL;
	char *d = dest;
	const char *s = src;
	for (--n; *s && s - src < n; *d++ = *s++)
		;
	*d = '\0';
	return dest;
}

char* lower(char *str, int len)
{
	if (!str || len <= 0)
		return str;
	int i = 0;
 	char *p = str;
	for (; i < len && *p; i++, p++)
		*p = tolower(*p);
	return str;
}

char* upper(char *str, int len)
{
	if (!str || len <= 0)
		return str;
	int i = 0;
 	char *p = str;
	for (; i < len && *p; i++, p++)
		*p = toupper(*p);
	return str;
}

char* trim(char *str, int *len)
{
	if (!str)
		return NULL;
	char *p = str;
	while (*p && (isblank(*p) || isspace(*p) || iscntrl(*p)))
		p++;
	if (!*p)
		return NULL;
	char *q = p + ((len && *len > 0) ? *len : strlen(p)) - 1;
	while (isblank(*q) || isspace(*q) || iscntrl(*q))
		q--;
	*(q + 1) = 0;
	if (len)
		*len = q + 1 - p;
	return p;
}

int start_with(const char *str, const char *trg, int icase)
{
	if (!str || !trg || !*str || !*trg)
		return 0;
	if (icase)
		while (tolower(*str) == tolower(*trg) && *str && *trg)
			str++, trg++;
	else
		while (*str == *trg && *str && *trg)
			str++, trg++;
	return *trg == '\0';
}

static const char __uri_chars[256] = {
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 1, 0, 0, 1, 0, 0, 1,   1, 1, 1, 0/* '+' */, 1, 1, 1, 0/* '\' */,
	1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 0, 0, 0/* encode '=' */, 0, 0,
	/* 64 */
	1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 0, 0, 0, 0, 1,
	0, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 0, 0, 0, 1, 0,
	/* 128 */
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	/* 192 */
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,
};

char* url_encode(buf_t *buf, const char *url)
{
	assert(buf && url);
	buf_reset(buf);

	unsigned char *p = (unsigned char*)url;
	for (; *p; p++) {
		if (__uri_chars[*p])
			buf_append(buf, (const char*)p, 1);
		else
			buf_printf_ext(buf, "%%%02x", *p);
	}
	return buf->data;
}

char* url_decode(buf_t *buf, const char *url)
{
	assert(buf && url);
	buf_reset(buf);

	char c;
	unsigned char *p = (unsigned char*)url;
	for (; *p; p++) {
		if (*p == '%' && isxdigit(*(p+1)) && isxdigit(*(p+2))) {
			char tmp[] = { *(p+1), *(p+2), '\0' };
			c = (char)strtol(tmp, NULL, 16);
			p += 2;
		} else if (*p == '+')
			c = ' ';
		else
			c = *p;
		buf_append(buf, &c, 1);
	}
	return buf->data;
}

static unsigned char* __md5(const void *input, int len, unsigned char output[16])
{
	MD5_CTX c;
	MD5_Init(&c);
	MD5_Update(&c, input, len);
	MD5_Final(output, &c);
	return output;
}

char* md5(const char *input, int len, char output[33])
{
	unsigned char uc[16] = {0};
	__md5(input, len, uc);

	int i = 0;
	char *p = output;
	for (; i < 16; i++, p += 2)
		sprintf(p, "%02x", uc[i]);
	output[32] = '\0';
	return output;
}

static unsigned char* __sha1(const void *input, int len, unsigned char output[20])
{
	SHA_CTX c;
	SHA1_Init(&c);
	SHA1_Update(&c, input, len);
	SHA1_Final(output, &c);
	return output;
}

char* sha1(const char *input, int len, char output[41])
{
	unsigned char uc[20] = {0};
	__sha1(input, len, uc);

	int i = 0;
	char *p = output;
	for (; i < 20; i++, p += 2)
		sprintf(p, "%02x", uc[i]);
	output[40] = '\0';
	return output;
}

static unsigned char* __sha256(const void *input, int len, unsigned char output[32])
{
	SHA256_CTX c;
	SHA256_Init(&c);
	SHA256_Update(&c, input, len);
	SHA256_Final(output, &c);
	return output;
}

char* sha256(const char *input, int len, char output[65])
{
	unsigned char uc[32] = {0};
	__sha256(input, len, uc);

	int i = 0;
	char *p = output;
	for (; i < 32; i++, p += 2)
		sprintf(p, "%02x", uc[i]);
	output[64] = '\0';
	return output;
}

static unsigned char* __sha512(const void *input, int len, unsigned char output[64])
{
	SHA512_CTX c;
	SHA512_Init(&c);
	SHA512_Update(&c, input, len);
	SHA512_Final(output, &c);
	return output;
}

char* sha512(const char *input, int len, char output[129])
{
	unsigned char uc[64] = {0};
	__sha512(input, len, uc);

	int i = 0;
	char *p = output;
	for (; i < 64; i++, p += 2)
		sprintf(p, "%02x", uc[i]);
	output[128] = '\0';
	return output;
}

/*
 * ptype 1: PKCS7Padding / PKCS5Padding model
 * 数据长度不对齐时填充差额个个数的字节、且每个字节值为差额数值本身；
 * 对齐时继续填充多一个块（bsize）字节，且每个字节值为bsize数值本身
 * 相对繁琐但解密时可根据最后一个字节数据明确知道最后多长的字节为填充信息，可获取准确的真正数据
 *
 * ptype 0: ZeroPadding model
 * 数据长度不对齐时使用0填充，否则不填充（ptype 1不同，对齐时也继续填充一个块）
 * 简单但解密时可能无法真正确定末尾系列0为真正数据还是填充数据
 */
static int padding_len(int ptype, int bsize, int *len)
{
	int orilen = *len;
	if (ptype)
		*len = (orilen / bsize + 1) * bsize;
	else
		*len = (orilen + bsize - 1) / bsize * bsize;
	return *len - orilen;
}

DES_KEY* des_key_new(const char *key, int check)
{
	DES_cblock dc;
	DES_string_to_key(key, &dc);
	DES_KEY *dk = xcalloc(1, sizeof(*dk));
	if (!check)
		DES_set_key_unchecked(&dc, dk);
	else if (DES_set_key_checked(&dc, dk) < 0) {
		free(dk);
		return NULL;
	}
	return dk;
}

int des_plen(int ptype, int *len)
{
	return padding_len(ptype, 8, len);
}

int wdes_ecb_encrypt(DES_KEY *dk, const char *input, char *output)
{
	DES_ecb_encrypt((const_DES_cblock*)input, (DES_cblock*)output, dk, DES_ENCRYPT);
	return 0;
}

int wdes_ecb_decrypt(DES_KEY *dk, const char *input, char *output)
{
	DES_ecb_encrypt((const_DES_cblock*)input, (DES_cblock*)output, dk, DES_DECRYPT);
	return 0;
}

int wdes_cbc_encrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output)
{
	DES_cbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, dk, (DES_cblock*)iv, DES_ENCRYPT);
	return 0;
}

int wdes_cbc_decrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output)
{
	DES_cbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, dk, (DES_cblock*)iv, DES_DECRYPT);
	return 0;
}

int wdes_ncbc_encrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output)
{
	DES_ncbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, dk, (DES_cblock*)iv, DES_ENCRYPT);
	return 0;
}

int wdes_ncbc_decrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output)
{
	DES_ncbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, dk, (DES_cblock*)iv, DES_DECRYPT);
	return 0;
}

void des_key_free(void *dk)
{
	free((DES_KEY*)dk);
}

AES_KEY* aes_encrypt_key(const char *key, int klen)
{
	if (klen == 16)
		klen = 128;
	else if (klen == 24)
		klen = 192;
	else if (klen == 32)
		klen = 256;
	else
		return NULL;

	AES_KEY *ak = xcalloc(1, sizeof(*ak));
	if (AES_set_encrypt_key((const unsigned char*)key, klen, ak) < 0) {
		free(ak);
		return NULL;
	}
	return ak;
}

AES_KEY* aes_decrypt_key(const char *key, int klen)
{
	if (klen == 16)
		klen = 128;
	else if (klen == 24)
		klen = 192;
	else if (klen == 32)
		klen = 256;
	else
		return NULL;

	AES_KEY *ak = xcalloc(1, sizeof(*ak));
	if (AES_set_decrypt_key((const unsigned char*)key, klen, ak) < 0) {
		free(ak);
		return NULL;
	}
	return ak;
}

void aes_key_free(void *ak)
{
	free((AES_KEY*)ak);
}

int aes_plen(int ptype, int *len)
{
	return padding_len(ptype, AES_BLOCK_SIZE, len);
}

int aes_ecb_encrypt(AES_KEY *ak, const char *input, char *output)
{
	AES_ecb_encrypt((const unsigned char*)input, (unsigned char*)output, ak, AES_ENCRYPT);
	return 0;
}

int aes_ecb_decrypt(AES_KEY *ak, const char *input, char *output)
{
	AES_ecb_encrypt((const unsigned char*)input, (unsigned char*)output, ak, AES_DECRYPT);
	return 0;
}

int aes_cbc_encrypt(AES_KEY *ak, char iv[16], const char *input, int len, char *output)
{
	AES_cbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, ak, (unsigned char*)iv, AES_ENCRYPT);
	return 0;
}

int aes_cbc_decrypt(AES_KEY *ak, char iv[16], const char *input, int len, char *output)
{
	AES_cbc_encrypt((const unsigned char*)input, (unsigned char*)output, len, ak, (unsigned char*)iv, AES_DECRYPT);
	return 0;
}

RSA* rsa_new_from_file(const char *keyfile, const char *keypwd)
{
	FILE *fp = fopen(keyfile, "r");
	if (!fp)
		return NULL;
	char h[64];
	if (!fgets(h, sizeof(h), fp)) {
		fclose(fp);
		return NULL;
	}
	fseek(fp, 0, SEEK_SET);

	RSA *rsa = NULL;
	char *p = strstr(h, "-----BEGIN RSA PRIVATE KEY-----");
	if (p && p == h) {
		rsa = PEM_read_RSAPrivateKey(fp, NULL, NULL, (void*)keypwd);
		goto __end;
	}

	p = strstr(h, "-----BEGIN RSA PUBLIC KEY-----");
	if (p && p == h) {
		rsa = PEM_read_RSAPublicKey(fp, NULL, NULL, (void*)keypwd);
		goto __end;
	}

	p = strstr(h, "-----BEGIN PUBLIC KEY-----");
	if (p && p == h)
		rsa = PEM_read_RSA_PUBKEY(fp, NULL, NULL, (void*)keypwd);

__end:
	fclose(fp);
	return rsa;
}

RSA* rsa_new_from_mem(const char *key, const char *keypwd)
{
	if (!key)
		return NULL;
	BIO *bio = BIO_new_mem_buf((void*)key, strlen(key));

	RSA *rsa = NULL;
	char *p = strstr(key, "-----BEGIN RSA PRIVATE KEY-----");
	if (p && p == key) {
		rsa = PEM_read_bio_RSAPrivateKey(bio, NULL, NULL, (void*)keypwd);
		goto __end;
	}

	p = strstr(key, "-----BEGIN RSA PUBLIC KEY-----");
	if (p && p == key) {
		rsa = PEM_read_bio_RSAPublicKey(bio, NULL, NULL, (void*)keypwd);
		goto __end;
	}

	p = strstr(key, "-----BEGIN PUBLIC KEY-----");
	if (p && p == key)
		rsa = PEM_read_bio_RSA_PUBKEY(bio, NULL, NULL, (void*)keypwd);

__end:
	BIO_free_all(bio);
	return rsa;
}

int rsa_encrypt(RSA *public_rsa, const char *input, int len, buf_t *output)
{
	int rsa_len = RSA_size(public_rsa);
	int once_max = rsa_len - RSA_PKCS1_PADDING_SIZE;
	unsigned char enc[rsa_len], *p = (unsigned char*)input;

	buf_reset(output);
	while (len > 0) {
		int input_len = len > once_max ? once_max : len;
		int enc_len = RSA_public_encrypt(input_len, p, enc, public_rsa, RSA_PKCS1_PADDING);
		if (enc_len < 0) {
			buf_reset(output);
			break;
		}
		buf_append(output, (char*)enc, enc_len);
		p += input_len;
		len -= input_len;
	}
	return output->offset;
}

int rsa_decrypt(RSA *private_rsa, const char *input, int len, buf_t *output)
{
	int rsa_len = RSA_size(private_rsa);
	int once_max = rsa_len;
	unsigned char dec[rsa_len], *p = (unsigned char*)input;

	buf_reset(output);
	while (len > 0) {
		int input_len = len > once_max ? once_max : len;
		int dec_len = RSA_private_decrypt(input_len, p, dec, private_rsa, RSA_PKCS1_PADDING);
		if (dec_len < 0) {
			buf_reset(output);
			break;
		}
		buf_append(output, (char*)dec, dec_len);
		p += input_len;
		len -= input_len;
	}
	return output->offset;
}

int rsa_sign(RSA *private_rsa, int type, const char *input, int len, char *sign)
{
	if (!input || len <= 0 || !sign)
		return 0;

	unsigned char uc[64] = {0};
	int ret = 0, rsa_len = RSA_size(private_rsa);

	switch (type) {
	case NID_md5:
		__md5(input, len, uc);
		ret = RSA_sign(NID_md5, uc, 16, (unsigned char*)sign, (unsigned int*)&rsa_len, private_rsa);
		break;

	case NID_sha1:
		__sha1(input, len, uc);
		ret = RSA_sign(NID_sha1, uc, 20, (unsigned char*)sign, (unsigned int*)&rsa_len, private_rsa);
		break;

	case NID_sha256:
		__sha256(input, len, uc);
		ret = RSA_sign(NID_sha256, uc, 32, (unsigned char*)sign, (unsigned int*)&rsa_len, private_rsa);
		break;

	case NID_sha512:
		__sha512(input, len, uc);
		ret = RSA_sign(NID_sha512, uc, 64, (unsigned char*)sign, (unsigned int*)&rsa_len, private_rsa);
		break;
	}
	return ret > 0 ? rsa_len : 0;
}

int rsa_verify(RSA *public_rsa, int type, const char *input, int len, const char *sign, int signlen)
{
	if (!input || len <= 0 || !sign || signlen <= 0)
		return 0;

	int ret = 0;
	unsigned char uc[64] = {0};

	switch (type) {
	case NID_md5:
		__md5(input, len, uc);
		ret = RSA_verify(NID_md5, uc, 16, (unsigned char*)sign, signlen, public_rsa);
		break;

	case NID_sha1:
		__sha1(input, len, uc);
		ret = RSA_verify(NID_sha1, uc, 20, (unsigned char*)sign, signlen, public_rsa);
		break;

	case NID_sha256:
		__sha256(input, len, uc);
		ret = RSA_verify(NID_sha256, uc, 32, (unsigned char*)sign, signlen, public_rsa);
		break;

	case NID_sha512:
		__sha512(input, len, uc);
		ret = RSA_verify(NID_sha512, uc, 64, (unsigned char*)sign, signlen, public_rsa);
		break;
	}
	return ret;
}

void rsa_free(void *rsa)
{
	RSA_free((RSA*)rsa);
}

char* gzip(const char *input, int len, buf_t *output)
{
	if (!input || len <= 0 || !output)
		return NULL;

	z_stream s;
	memset(&s, 0, sizeof(z_stream));
	if (deflateInit2(&s, Z_DEFAULT_COMPRESSION, Z_DEFLATED, 16+MAX_WBITS, 8, Z_DEFAULT_STRATEGY) != Z_OK)
		return NULL;

	char buf[512];
	s.next_in = (Bytef*)input;
	s.avail_in = len;
	buf_reset(output);
	do {
		s.next_out = (Bytef*)buf;
		s.avail_out = sizeof(buf);
		if (deflate(&s, Z_FINISH) == Z_STREAM_ERROR) {
			buf_reset(output);
			break;
		}
		buf_append(output, buf, sizeof(buf) - s.avail_out);
	} while (s.avail_out == 0)
		;
	deflateEnd(&s);

	return output->data;
}

char* gunzip(const char *input, int len, buf_t *output)
{
	if (!input || len <= 0 || !output)
		return NULL;

	z_stream s;
	memset(&s, 0, sizeof(z_stream));
	if (inflateInit2(&s, 16+MAX_WBITS) != Z_OK)
		return NULL;

	char buf[512];
	s.next_in = (Bytef*)input;
	s.avail_in = len;
	buf_reset(output);
	do {
		s.next_out = (Bytef*)buf;
		s.avail_out = sizeof(buf);
		if (inflate(&s, Z_FINISH) == Z_STREAM_ERROR) {
			buf_reset(output);
			break;
		}
		buf_append(output, buf, sizeof(buf) - s.avail_out);
	} while (s.avail_out == 0)
		;
	inflateEnd(&s);

	return output->data;
}

int zip(const char *src, int len, char *dst, qlz_state_compress *state)
{
	state->stream_counter = 0;
	return qlz_compress(src, dst, len, state);
}

int unzip(const char *src, char *dst, qlz_state_decompress *state)
{
	state->stream_counter = 0;
	return qlz_decompress(src, dst, state);
}

int ziplen(const char *compressed)
{
	/* Get the meta length from the compressed header. */
	return (int)qlz_size_compressed(compressed);
}

int unziplen(const char *compressed)
{
	/* Get the meta length from the compressed header. */
	return (int)qlz_size_decompressed(compressed);
}
