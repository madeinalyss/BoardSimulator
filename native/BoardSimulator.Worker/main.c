#include <stdio.h>

int main(void) {
    fputs("Worker started\n", stderr);
    fputs("HELLO 1\n", stdout);
    fflush(stdout);
    return 0;
}
