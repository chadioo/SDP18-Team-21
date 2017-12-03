#include <io.h>
#include <avr/interrupt.h>
#include <stdlib.h>
#include <string.h>
#include <avr/io.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>
#include <util/delay.h>

/*
Import Bosh Code 
https://github.com/BoschSensortec/BMI160_driver/blob/master/README.md
*/
#include "bmi160.h"
#include "bmi160.c"



int main(void)
{

	// please double check my work for pointer errors

	/*
	Specify parameters for user_i2c_read
	*/
	uint8_t read_dev_addr = 0;
	uint8_t read_reg_addr = 0;
	uint8_t *read_data;
	read_data = 0;
	uint16_t read_len = 0;
	bmi160_com_fptr_t *user_i2c_read;
	// may need to swap || with &&
	user_i2c_read = (read_dev_addr || read_reg_addr || *read_data || read_len);

	/*
	Specify parameters for user_i2c_write
	*/
	uint8_t write_dev_addr = 0;
	uint8_t write_reg_addr = 0;
	uint8_t *write_data;
	write_data = 0;
	uint16_t write_len = 0;
	bmi160_com_fptr_t *user_i2c_write;
	// may need to swap || with &&
	user_i2c_write = (write_dev_addr || write_reg_addr || *write_data || write_len);

	/*
	Specify parameters for user_delay_ms
	*/
	bmi160_delay_fptr_t *user_delay_ms;
	user_delay_ms = 0x0;

	/*
	Sample Init Code for I2C
	*/
	struct bmi160_dev sensor;
	sensor.id = BMI160_I2C_ADDR;
	sensor.interface = BMI160_I2C_INTF;
	sensor.read = *user_i2c_read;
	sensor.write = *user_i2c_write;
	sensor.delay_ms = *user_delay_ms;

	int8_t rslt = BMI160_OK;
	rslt = bmi160_init(&sensor);

	/* After the above function call, accel and gyro parameters in the device structure 
	are set with default values, found in the datasheet of the sensor */


}
