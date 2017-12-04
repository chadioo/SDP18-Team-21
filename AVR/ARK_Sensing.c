////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// SDP Team 21 - ARK												      //
// Matteo Bolognese, Jackie Lagasse, Chad Klinefleter, Ethan Miller		  //
// Last Updated: December 3, 2017										  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////

#include "i2c.h"			// ATMega32 I2C Registers

#include <avr/interrupt.h>	// Interrupt code (used in Bluetooth oferflow)

#include "bmi160.h"			// BMI160 Methods
#include "bmi160.c"			// BMI160 Methods
#include "bmi160_defs.h"	// BMI160 Methods

////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// Bosch BMI160 Commands												  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////



////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// ATMega32 I2C Commands												  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////


void init_clock(){

	/* Initialize clock settings */
	TWSR=0x00; // set presca1er bits to 1 (0x00=1, 0x01=4, 0x02=16, 0x03=64)
    TWBR=0x0D; // SCL frequency

}


void send_start_signal(){

	/* Send START condition 
	TWCR = TWI Control Register
	TWINT = TWI interrupt flag ( bit 7 on TWCR )
	TWSTA = START condition bit ( bit 5 on TWCR )
	TWEN = TWI enable bit ( bit 2 on TWCR )
	*/
	TWCR = (1<<TWINT)|(1<<TWSTA)|(1<<TWEN);

	/* Wait until START condition has been transmitted 
	Wait until the TWINT flag is set */
	while (!(TWCR & (1<<TWINT)))
		;
}

void send_end_signal(){

	/* Transmit STOP condition */
	TWCR = (1<<TWINT)|(1<<TWEN)|
	(1<<TWSTO);

}

void read_write_data(int DATA, int RW){

	/* Load DATA+RW into TWDR Register 
	This is the DATA + R/W bit 
	RW=0 is write, RW=1 is read */
	DATA = (DATA<<1) | RW;
	TWDR = DATA;

	/* Clear TWINT bit in TWCR to start transmission of address */
	TWCR = (1<<TWINT) | (1<<TWEN);

	/* Wait for TWINT Flag set
	This indicates that the SLA+W has been transmitted
	and ACK/NACK has been received. */
	while (!(TWCR & (1<<TWINT)))
		;
}

void print_trace(){

	/* Develop method to print stack trace or throw some other error */

}


void check_status_register(int REGISTER){

	/* Check value of TWI status register, mask prescaler bits
	If status is different from REGISTER, go to print_trace 
	TWSR =  TWI Status Register
	TWI Status is set on bits 7:3 of TWSR, = 0x78
	*/
	if ((TWSR & 0xF8) != REGISTER)
		print_trace(); // define how you want to handle this error somewhere

}


////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// HC-05 Bluetooth Commands												  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////


// Method triggered by overflow
ISR(TIMER1_OVF_vect){

    PORTB ^= 0xFF;			//toggle PORTB values
}

// Data from Bluetooth TX given to USART RX
unsigned char USART_Receive( void ){

	/* Wait for data to be received */
	while ( !(UCSRA & (1<<RXC)) ) {  }
	/* Get and return received data from buffer */
	return UDR;
}

// Data from USART TX given to Bluetooth RX
void USART_Transmit( unsigned char data ){

	/* Wait for empty transmit buffer */
	while ( !( UCSRA & (1<<UDRE)) ) {  }

	/* Put data into buffer, sends the data */
	UDR = data;
}

/* Send string of USART data function */ 
void USART_SendString(char *str){

	int i=0;			
	/* Send each char of string till the NULL */														
	while (str[i]!=0){
		USART_Transmit( str[i] );
		i++;
	}
}

// Flush USART
void USART_Flush( void ){

	unsigned char dummy;
	while ( UCSRA & (1<<RXC) ) dummy = UDR;
}

// Initialize USART
void USART_Init( unsigned int baud ){
	/* Set baud rate */
	UBRRH = (unsigned char)(baud>>8);
	UBRRL = (unsigned char)baud;
	UCSRA |= (1<<U2X);

	/* Enable receiver and transmitter */
	UCSRB = (1<<RXEN)|(1<<TXEN);

	/* Set frame format: 8data, 2stop bit */
	UCSRC = (1<<URSEL)|(1<<USBS)|(3<<UCSZ0);
}


void Bluetooth_Init(){

	USART_Init(12); // UBRR value for 9600
	char *cmd = "AT+UART=9600,2,0\r\n";
	while (*cmd != '\0'){
		USART_Transmit( *cmd );
		++cmd;
	}
}

void USART_Start_Timer(){

	TCCR1B |= (1 << CS11);	// Timer1 prescaler = 8
	TCNT1 = 0;				// Clear the timer counter
	TIMSK = (1 << TOIE1);	// Enable timer1 overflow interrupt(TOIE1)
	sei();					// Enable global interrupts
}

////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// Main Method															  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////


int main(void) {



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



	/* Select the Output data rate, range of Gyroscope sensor */
	sensor.gyro_cfg.odr = BMI160_GYRO_ODR_3200HZ;
	sensor.gyro_cfg.range = BMI160_GYRO_RANGE_2000_DPS;
	sensor.gyro_cfg.bw = BMI160_GYRO_BW_NORMAL_MODE;

	/* Select the power mode of Gyroscope sensor */
	sensor.gyro_cfg.power = BMI160_GYRO_NORMAL_MODE; 

	/* Set the sensor configuration */
	rslt = bmi160_set_sens_conf(&sensor);

	struct bmi160_sensor_data gyro;

	/* To read only Gyro data */
	rslt = bmi160_get_sensor_data(BMI160_GYRO_SEL, NULL, &gyro, &sensor);





	init_clock();

	send_start_signal();

	// check_status_register(START); 

	int slave_address = 0x68;				// default I2C address of BMI160
	read_write_data(slave_address,0);		// write slave_address

	// check_status_register(MT_SLA_ACK);

	int register_address = 0x0C;			// 0x0C is 1st byte of Gyro X axis register
	read_write_data(register_address,0);	// write register_address

	// check_status_register(MT_DATA_ACK);

	// Now we are going to read actual data from the sensor

	send_start_signal();
	read_write_data(slave_address,1);		// read slave_address

	// end program
	send_end_signal();







	// BLUETOOTH CODE

	/*
	Bluetooth_Init();
    
	// LED Light is Port B
	DDRB = 0xFF;			// Set PORTB as output
	PORTB = 0;				// Clear PORTB bits (turn the LEDs off)
    
	// Bluetooth is Port D
	DDRD = 0x01; 			// PD0 is RX so processor reads it, PD1 is TX so processor writes it

	while(1){
	
		char DATA_IN = USART_Receive();
		
		if(DATA_IN == '1') {

			PORTB |= 0xFF; // all the LEDs
			USART_SendString( "LED is on.\n" );
		}

		else if(DATA_IN == '0') {

			PORTB &= ~0xFF; // all the LEDs
			USART_SendString( "LED is off.\n" );
		}
		
		else if(DATA_IN == '\n' || DATA_IN == '\r');
		else USART_SendString( "Please select a proper option.\n" );

		// USART_Transmit( DATA_IN );
	*/

}



