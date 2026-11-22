#ifndef BOARDSIM_HAL_H
#define BOARDSIM_HAL_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef uint8_t hal_pin_t;

typedef enum {
    HAL_INPUT,
    HAL_INPUT_PULLUP,
    HAL_OUTPUT
} hal_pin_mode_t;

typedef enum {
    HAL_LOW = 0,
    HAL_HIGH = 1
} hal_level_t;

void hal_gpio_mode(hal_pin_t pin, hal_pin_mode_t mode);
void hal_gpio_write(hal_pin_t pin, hal_level_t level);
hal_level_t hal_gpio_read(hal_pin_t pin);
uint32_t hal_millis(void);

void setup(void);
void app_step(void);

#ifdef __cplusplus
}
#endif

#endif
