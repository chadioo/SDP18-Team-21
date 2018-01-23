////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// SDP Team 21 - ARK												      //
// Matteo Bolognese, Jackie Lagasse, Chad Klinefleter, Ethan Miller		  //
// Last Updated: December 30, 2017										  //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////


#include <avr/io.h>
#include <avr/interrupt.h>	// Interrupt code (used in Bluetooth overflow)
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include <util/delay.h>
#include <math.h>
#include <inttypes.h>			/* Include integer type header file */
#include "MPU6050_res_define.h"	/* Include MPU6050 register define file */
#include "I2C_Master_H_file.h"	/* Include I2C Master header file */

#define F_CPU 8000000UL		/* Define CPU */
#define SCL_CLK 100000L		/* Define SCL clock frequency */

#define SLAVE_WRITE_ADDRESS 0xD0
#define SLAVE_READ_ADDRESS 0xD1

#define SMPLRT_DIV 0x19
#define CONFIG 0x1A
#define GYRO_CONFIG 0x1B
#define ACCEL_CONFIG 0x1C
#define INT_PIN_CFG 0x37
#define INT_ENABLE 0x38
#define DMP_INT_STATUS 0x39
#define INT_STATUS 0x3A
#define ACCEL_XOUT_H 0x3B
#define ACCEL_XOUT_L 0x3C
#define ACCEL_YOUT_H 0x3D
#define ACCEL_YOUT_L 0x3E
#define ACCEL_ZOUT_H 0x3F
#define ACCEL_ZOUT_L 0x40
#define TEMP_OUT_H 0x41
#define TEMP_OUT_L 0x42
#define GYRO_XOUT_H 0x43
#define GYRO_XOUT_L 0x44
#define GYRO_YOUT_H 0x45
#define GYRO_YOUT_L 0x46
#define GYRO_ZOUT_H 0x47
#define GYRO_ZOUT_L 0x48
#define PWR_MGMT_1 0x6B
#define PWR_MGMT_2 0x6C

float Acc_x,Acc_y,Acc_z,Temperature,Gyro_x,Gyro_y,Gyro_z;


////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// Analog Commands												          //
// 																		  //
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////


unsigned int read_adc(int axis){

	if(axis==1){		// z axis is PA0
		ADMUX = 0b10100000;
	}
	else if(axis==2){	// y axis is PA1
		ADMUX = 0b10100001;
	}
	else if(axis==3){	// x axis is PA2
		ADMUX = 0b10100010;
	}
	else{
		return 0;
	}
	ADMUX = (1<<REFS0);	// set mux
	ADCSRA = (1<<ADEN)|(1<<ADPS2)|(1<<ADPS0);	// divided by prescale of 32
	ADCSRA|= (1<<ADSC);	// clear ADSC by writing one to it
	while(!(ADCSRA&(1<<ADSC)))	// wait for conversion to complete
		;
	return(ADC);		// retuens 10 bit unsigned number
}


////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// MPU6050 Commands												  		  //
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
	TWSR = 0x00; // set presca1er bits to 1 (0x00=1, 0x01=4, 0x02=16, 0x03=64)
    TWBR = ((F_CPU/SCL_CLK)-16)/(2*pow(4,(TWSR&((1<<TWPS0)|(1<<TWPS1))))); // SCL frequency

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

void send_stop_signal(){

	/* Transmit STOP condition */
	TWCR = (1<<TWINT)|(1<<TWEN)|
	(1<<TWSTO);

}

char read_write_data(int DATA, int RW){

	DATA = DATA >> 1;

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
	return (char) TWDR;
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

char read_ack(){											/* I2C read ack function */

	TWCR=(1<<TWEN)|(1<<TWINT)|(1<<TWEA);					/* Enable TWI, generation of ack and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (read operation) */
	return TWDR;											/* Return received data */
}	

char read_nack(){											/* I2C read nack function */

	TWCR=(1<<TWEN)|(1<<TWINT);								/* Enable TWI and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (read operation) */
	return TWDR;											/* Return received data */
}	

////////////////////////////////////////////////////////////

uint8_t I2C_Start(char slave_write_address)						/* I2C start function */
{
	uint8_t status;											/* Declare variable */
	TWCR = (1<<TWSTA)|(1<<TWEN)|(1<<TWINT);					/* Enable TWI, generate start condition and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (start condition) */
	status = TWSR & 0xF8;									/* Read TWI status register with masking lower three bits */
	if (status != 0x08)										/* Check weather start condition transmitted successfully or not? */
	return 0;												/* If not then return 0 to indicate start condition fail */
	TWDR = slave_write_address<<1;								/* If yes then write SLA+W in TWI data register */
	TWCR = (1<<TWEN)|(1<<TWINT);							/* Enable TWI and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (Write operation) */
	status = TWSR & 0xF8;									/* Read TWI status register with masking lower three bits */	
	if (status == 0x18)										/* Check weather SLA+W transmitted & ack received or not? */
	return 1;												/* If yes then return 1 to indicate ack received i.e. ready to accept data byte */
	if (status == 0x20)										/* Check weather SLA+W transmitted & nack received or not? */
	return 2;												/* If yes then return 2 to indicate nack received i.e. device is busy */
	else
	return 3;												/* Else return 3 to indicate SLA+W failed */
}

uint8_t I2C_Repeated_Start(char slave_read_address)			/* I2C repeated start function */
{
	uint8_t status;											/* Declare variable */
	TWCR = (1<<TWSTA)|(1<<TWEN)|(1<<TWINT);					/* Enable TWI, generate start condition and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (start condition) */
	status = TWSR & 0xF8;									/* Read TWI status register with masking lower three bits */
	if (status != 0x10)										/* Check weather repeated start condition transmitted successfully or not? */
	return 0;												/* If no then return 0 to indicate repeated start condition fail */
	TWDR = slave_read_address;								/* If yes then write SLA+R in TWI data register */
	TWCR = (1<<TWEN)|(1<<TWINT);							/* Enable TWI and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (Write operation) */
	status = TWSR & 0xF8;									/* Read TWI status register with masking lower three bits */
	if (status == 0x40)										/* Check weather SLA+R transmitted & ack received or not? */
	return 1;												/* If yes then return 1 to indicate ack received */ 
	if (status == 0x20)										/* Check weather SLA+R transmitted & nack received or not? */
	return 2;												/* If yes then return 2 to indicate nack received i.e. device is busy */
	else
	return 3;												/* Else return 3 to indicate SLA+W failed */
}

void I2C_Stop()												/* I2C stop function */
{
	TWCR=(1<<TWSTO)|(1<<TWINT)|(1<<TWEN);					/* Enable TWI, generate stop condition and clear interrupt flag */
	while(TWCR & (1<<TWSTO));								/* Wait until stop condition execution */ 
}

void I2C_Start_Wait(char slave_write_address)			/* I2C start wait function */
{
	uint8_t status;											/* Declare variable */
	while (1)
	{
		TWCR = (1<<TWSTA)|(1<<TWEN)|(1<<TWINT);				/* Enable TWI, generate start condition and clear interrupt flag */
		while (!(TWCR & (1<<TWINT)));						/* Wait until TWI finish its current job (start condition) */
		status = TWSR & 0xF8;								/* Read TWI status register with masking lower three bits */
		if (status != 0x08)									/* Check weather start condition transmitted successfully or not? */
		continue;											/* If no then continue with start loop again */
		TWDR = slave_write_address;							/* If yes then write SLA+W in TWI data register */
		TWCR = (1<<TWEN)|(1<<TWINT);						/* Enable TWI and clear interrupt flag */
		while (!(TWCR & (1<<TWINT)));						/* Wait until TWI finish its current job (Write operation) */
		status = TWSR & 0xF8;								/* Read TWI status register with masking lower three bits */
		if (status != 0x18 )								/* Check weather SLA+W transmitted & ack received or not? */
		{
			I2C_Stop();										/* If not then generate stop condition */
			continue;										/* continue with start loop again */
		}
		break;												/* If yes then break loop */
	}
}

uint8_t I2C_Write(char data)								/* I2C write function */
{
	uint8_t status;											/* Declare variable */
	TWDR = data;											/* Copy data in TWI data register */
	TWCR = (1<<TWEN)|(1<<TWINT);							/* Enable TWI and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (Write operation) */
	status = TWSR & 0xF8;									/* Read TWI status register with masking lower three bits */
	if (status == 0x28)										/* Check weather data transmitted & ack received or not? */
	return 0;												/* If yes then return 0 to indicate ack received */
	if (status == 0x30)										/* Check weather data transmitted & nack received or not? */
	return 1;												/* If yes then return 1 to indicate nack received */
	else
	return 2;												/* Else return 2 to indicate data transmission failed */
}

char I2C_Read_Ack()										/* I2C read ack function */
{
	TWCR=(1<<TWEN)|(1<<TWINT)|(1<<TWEA);					/* Enable TWI, generation of ack and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (read operation) */
	return TWDR;											/* Return received data */
}	

char I2C_Read_Nack()										/* I2C read nack function */
{
	TWCR=(1<<TWEN)|(1<<TWINT);								/* Enable TWI and clear interrupt flag */
	while (!(TWCR & (1<<TWINT)));							/* Wait until TWI finish its current job (read operation) */
	return TWDR;											/* Return received data */
}	

////////////////////////////////////////////////////////////

void MPU6050_Init()										/* Gyro initialization function */
{
	_delay_ms(150);										/* Power up time >100ms */
	I2C_Start_Wait(0xD0);								/* Start with device write address */
	I2C_Write(SMPLRT_DIV);								/* Write to sample rate register */
	I2C_Write(0x07);									/* 1KHz sample rate */
	I2C_Stop();

	I2C_Start_Wait(0xD0);
	I2C_Write(PWR_MGMT_1);								/* Write to power management register */
	I2C_Write(0x01);									/* X axis gyroscope reference frequency */
	I2C_Stop();

	I2C_Start_Wait(0xD0);
	I2C_Write(CONFIG);									/* Write to Configuration register */
	I2C_Write(0x00);									/* Fs = 8KHz */
	I2C_Stop();

	I2C_Start_Wait(0xD0);
	I2C_Write(GYRO_CONFIG);								/* Write to Gyro configuration register */
	I2C_Write(0x18);									/* Full scale range +/- 2000 degree/C */
	I2C_Stop();

	I2C_Start_Wait(0xD0);
	I2C_Write(INT_ENABLE);								/* Write to interrupt enable register */
	I2C_Write(0x01);
	I2C_Stop();
}

void MPU_Start_Loc()
{
	I2C_Start_Wait(0xD0);								/* I2C start with device write address */
	I2C_Write(ACCEL_XOUT_H);							/* Write start location address from where to read */ 
	I2C_Repeated_Start(0xD1);							/* I2C start with device read address */
}

void Read_RawValue()
{
	MPU_Start_Loc();									/* Read Gyro values */
	Acc_x = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Acc_y = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Acc_z = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Temperature = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Gyro_x = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Gyro_y = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Ack());
	Gyro_z = (((int)I2C_Read_Ack()<<8) | (int)I2C_Read_Nack());
	I2C_Stop();
}

////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
// 																		  //
// ATMega32 USART Commands												  //
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


	// STEP 1: Establish Bluetooth / USART Connection

	Bluetooth_Init();
	//USART_SendString( "step 1" ); 

	// STEP 2: Initialize Variables

	char buffer[20], float_[10];
	float Xa,Ya,Za,t;
	float Xg=0,Yg=0,Zg=0;
	//USART_SendString( "step 2" );

	// STEP 3: Establish I2C Connection

	init_clock();
	//USART_SendString( "step 3" );

	// STEP 4: Configure MPU6050+

	/*
	CHAD: note to self, this function call is the issue.
	Things break here. Look into the parts of this function.
	*/

	MPU6050_Init();
	//USART_SendString( "step 4" );  

/*
	send_start_signal();
	read_write_data(SLAVE_WRITE_ADDRESS,0);
	read_write_data(SMPLRT_DIV,0);	
	read_write_data(0x07,0);
	send_stop_signal();
*/
  
/*	// LED Light is Port B
	DDRB = 0xFF;			// Set PORTB as output
	PORTB = 0;				// Clear PORTB bits (turn the LEDs off)
    
	// Bluetooth is Port D
	DDRD = 0x01; 			// PD0 is RX so processor reads it, PD1 is TX so processor writes it
*/

	// STEP 5: Send Data To Phone Over Bluetooth
///*
	while(1){
	
		//char DATA_IN = USART_Receive();
		
		//if(DATA_IN == '1') {

			Read_RawValue();

			Xa = Acc_x/16384.0;								
			Ya = Acc_y/16384.0;
			Za = Acc_z/16384.0;
		
			Xg = Gyro_x/16.4;
			Yg = Gyro_y/16.4;
			Zg = Gyro_z/16.4;

			t = (Temperature/340.00)+36.53;			

			dtostrf( Xa, 3, 2, float_ );				
			sprintf(buffer," Ax = %s g\t",float_);
			USART_SendString(buffer);

			dtostrf( Ya, 3, 2, float_ );
			sprintf(buffer," Ay = %s g\t",float_);
			USART_SendString(buffer);
		
			dtostrf( Za, 3, 2, float_ );
			sprintf(buffer," Az = %s g\t",float_);
			USART_SendString(buffer);

			dtostrf( t, 3, 2, float_ );
			sprintf(buffer," T = %s%cC\t",float_,0xF8);        
			USART_SendString(buffer);

			dtostrf( Xg, 3, 2, float_ );
			sprintf(buffer," Gx = %s%c/s\t",float_,0xF8);
			USART_SendString(buffer);

			dtostrf( Yg, 3, 2, float_ );
			sprintf(buffer," Gy = %s%c/s\t",float_,0xF8);
			USART_SendString(buffer);
		
			dtostrf( Zg, 3, 2, float_ );
			sprintf(buffer," Gz = %s%c/s\r\n",float_,0xF8);
			USART_SendString(buffer);

		//}

		//else if(DATA_IN == '0') {

			//PORTB &= ~0xFF; // all the LEDs
			//USART_SendString( "LED is off.\n" );
		//}		

	}
//*/

	// STEP 6: Terminate I2C Connection	

	//send_stop_signal();


}
