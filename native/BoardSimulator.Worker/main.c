#include <stdio.h>
#include <string.h>
#include "boardsim_hal.h"

int main(void) {
    fputs("Worker started\n", stderr);
    fputs("HELLO 1\n", stdout);
    fflush(stdout);
    char command[64];
    int initialized = 0;
    while (fgets(command, sizeof command, stdin) != NULL) {
        if (strchr(command, '\n') == NULL) {
            fputs("Protocol error: incomplete or oversized command\n", stderr);
            return 2;
        }
        fputs("Received: ", stderr);
        fputs(command, stderr);
        command[strcspn(command, "\r\n")] = '\0';
        if (strcmp(command, "PING") == 0) {
            fputs("PONG\n", stdout);
        }
        else if (strcmp(command, "PONG") == 0) {
            fputs("PING\n", stdout);
        }
        else if (strcmp(command, "SETUP") == 0) {
            if (initialized) {
                fputs("Protocol error: setup already completed\n", stderr);
                return 2;
            }

            setup();
            initialized = 1;
            fputs("DONE SETUP\n", stdout);
        }
        else if (strcmp(command, "STEP") == 0) {
            if (!initialized) {
                fputs("Protocol error: setup required before step\n", stderr);
                return 2;
            }

            app_step();
            fputs("DONE STEP\n", stdout);
        }
        else {
            fputs("Unknown command\n", stderr);
            fputs("ERROR UNKNOWN_COMMAND\n", stdout);
        }
        fflush(stdout);
    }
    return 0;
}
