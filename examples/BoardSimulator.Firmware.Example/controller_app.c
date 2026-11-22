#include "boardsim_hal.h"

enum {
    BTN = 2,
    LED = 3
};

void setup(void) {
    hal_gpio_mode(BTN, HAL_INPUT_PULLUP);
    hal_gpio_mode(LED, HAL_OUTPUT);
    hal_gpio_write(LED, HAL_LOW);
}

void app_step(void) {
    if (hal_gpio_read(BTN) == HAL_LOW) {
        hal_gpio_write(LED, HAL_HIGH);
    }
    else {
        hal_gpio_write(LED, HAL_LOW);
    }
}
