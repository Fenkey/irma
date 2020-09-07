#ifndef __MISC_H__
#define __MISC_H__

#include "buf.h"
#include "quicklz.h"
#include <openssl/pem.h>
#include <openssl/des.h>
#include <openssl/aes.h>
#include <openssl/rsa.h>

#ifdef __cplusplus
extern "C" {
#endif

void		thread_sleep(int interval);
double		second_diff(const struct timeval *tb, const struct timeval *te);
int			today();
time_t		now(int *v);
time_t		sometime(int y, int m, int d, int h, int M, int s);
time_t		oneday(int y, int m, int d);
char*		time2string(time_t *time, char ymd[20]);

const char*	memstr(const char *buf, int blen, const char *str, int slen);
const char*	memcasestr(const char *buf, int blen, const char *str, int slen);
char*		sstrncpy(char *dest, const char *src, int n);
char*		lower(char *str, int len);
char*		upper(char *str, int len);
char*		trim(char *str, int *len);
int			start_with(const char *str, const char *trg, int icase);

char*		url_encode(buf_t *buf, const char *url);
char*		url_decode(buf_t *buf, const char *url);

char*		md5(const char *input, int len, char output[33]);
char*		sha1(const char *input, int len, char output[41]);
char*		sha256(const char *input, int len, char output[65]);
char*		sha512(const char *input, int len, char output[129]);

#define		DES_BLOCK_SIZE (sizeof(DES_cblock))
typedef		DES_key_schedule DES_KEY;
DES_KEY*	des_key_new(const char *key, int check);
void		des_key_free(void *dk);
int			des_plen(int ptype, int *len);
int			wdes_ecb_encrypt(DES_KEY *dk, const char *input, char *output);
int			wdes_ecb_decrypt(DES_KEY *dk, const char *input, char *output);
int			wdes_cbc_encrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output);
int			wdes_cbc_decrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output);
int			wdes_ncbc_encrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output);
int			wdes_ncbc_decrypt(DES_KEY *dk, char iv[8], const char *input, int len, char *output);

AES_KEY*	aes_encrypt_key(const char *key, int klen);
AES_KEY*	aes_decrypt_key(const char *key, int klen);
void		aes_key_free(void *ak);
int			aes_plen(int ptype, int *len);
int			aes_ecb_encrypt(AES_KEY *ak, const char *input, char *output);
int			aes_ecb_decrypt(AES_KEY *ak, const char *input, char *output);
int			aes_cbc_encrypt(AES_KEY *ak, char iv[16], const char *input, int len, char *output);
int			aes_cbc_decrypt(AES_KEY *ak, char iv[16], const char *input, int len, char *output);

RSA*		rsa_new_from_file(const char *keyfile, const char *keypwd);
RSA*		rsa_new_from_mem(const char *key, const char *keypwd);
int			rsa_encrypt(RSA *public_rsa, const char *input, int len, buf_t *output);
int			rsa_decrypt(RSA *private_rsa, const char *input, int len, buf_t *output);
int			rsa_sign(RSA *private_rsa, int type, const char *input, int len, char *sign);
int			rsa_verify(RSA *public_rsa, int type, const char *input, int len, const char *sign, int signlen);
void		rsa_free(void *rsa);

char*		gzip(const char *input, int len, buf_t *output);
char*		gunzip(const char *input, int len, buf_t *output);
int			zip(const char *src, int len, char *dst, qlz_state_compress *state);
int			unzip(const char *src, char *dst, qlz_state_decompress *state);
int			ziplen(const char *compressed);
int			unziplen(const char *compressed);

#ifdef __cplusplus
}
#endif

#endif
