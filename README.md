# ICopy
Support full/incremental/differential backup, file encrypt/decrypt and copy/move

## Example
### Full/Incremental/Differential Backup
ICopy source target /full

ICopy source target /incremental

ICopy source target /differential

### Encrypt/Decrypt
ICopy source target /encrypt

ICopy source targer /decrypt

### Copy(default)/Move
ICopy source target /copy
ICopy source target /move

### File Exclude
ICopy source target /exclude=.ext

### Combination
ICopy source target /full /encrypt

ICopy source target /move /exclude=.ext
