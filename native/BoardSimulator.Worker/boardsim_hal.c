#include "boardsim_hal.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void hal_gpio_mode(hal_pin_t pin, hal_pin_mode_t mode) {
    fprintf(stdout, "HAL MODE %u %d\n", (unsigned)pin, (int)mode);
    fflush(stdout);

    char response[64];
    if (fgets(response, sizeof response, stdin) == NULL || strcmp(response, "OK\n") != 0) {
        fputs("HAL mode request failed\n", stderr);
        exit(2);
    }
}

void hal_gpio_write(hal_pin_t pin, hal_level_t level) {
    fprintf(stdout, "HAL WRITE %u %d\n", (unsigned)pin, (int)level);
    fflush(stdout);

    char response[64];
    if (fgets(response, sizeof response, stdin) == NULL || strcmp(response, "OK\n") != 0) {
        fputs("HAL write request failed\n", stderr);
        exit(2);
    }
}

hal_level_t hal_gpio_read(hal_pin_t pin) {
    fprintf(stdout, "HAL READ %u\n", (unsigned)pin);
    fflush(stdout);

    char response[64];
    if (fgets(response, sizeof response, stdin) == NULL) {
        fputs("HAL read response missing\n", stderr);
        exit(2);
    }

    if (strcmp(response, "VALUE 0\n") == 0) {
        return HAL_LOW;
    }

    if (strcmp(response, "VALUE 1\n") == 0) {
        return HAL_HIGH;
    }

    fputs("Invalid HAL read response\n", stderr);
    exit(2);
}
