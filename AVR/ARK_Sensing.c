#include <avr/io.h>
#include <avr/interrupt.h>
#include <stdlib.h>
#include <string.h>
#include <avr/io.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>
#include <util/delay.h>
#include <compat/twi.h>

#include "delay.h"
#include "i2c.h"

int main(void) {

	/* Initialize clock settings */
	TWSR=0x00; // set presca1er bits to 1 (0x00=1, 0x01=4, 0x02=16, 0x03=64)
    TWBR=0x2A; // SCL frequency

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

	/* Check value of TWI status register, mask prescaler bits
	If status is different from START, go to ERROR 
	TWSR =  TWI Status Register
	TWI Status is set on bits 7:3 of TWSR, = 0x78
	*/
	/*
	if ((TWSR & 0xF8) != START)
		ERROR(); // define how you want to handle this error somewhere
	*/

	/* Load SLA+W into TWDR Register 
	This is the Slave Address + R/W bit*/
	int SLA_W = (0x68<<1) | 0;
	TWDR = SLA_W;

	/* Clear TWINT bit in TWCR to start transmission of address */
	TWCR = (1<<TWINT) | (1<<TWEN);

	/* Wait for TWINT Flag set
	This indicates that the SLA+W has been transmitted
	and ACK/NACK has been received. */
	while (!(TWCR & (1<<TWINT)))
		;

	/* Check value of TWI Status Register, mask prescaler bits
	If status different from MT_SLA_ACK go to ERROR */
	/*
	int MT_SLA_ACK = 0;

	if ((TWSR & 0xF8) != MT_SLA_ACK)
		ERROR();
	
	*/

	/* Load DATA into TWDR Register. */
	int DATA = 0x0C;
	TWDR = DATA;

	/* Clear TWINT bit in TWCR to start transmission of data */
	TWCR = (1<<TWINT) | (1<<TWEN);

	/* Wait until START condition has been transmitted 
	Wait until the TWINT flag is set */
	while (!(TWCR & (1<<TWINT)))
		;

	/* Check value of TWI Status Register, mask prescaler bits
	If status different from MT_DATA_ACK go to ERROR */
	/*
	int MT_DATA_ACK = 0;

	if ((TWSR & 0xF8) != MT_DATA_ACK)
		ERROR();
	
	*/


	// Now we are going to read actual data from the sensor



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

	/* Load SLA+W into TWDR Register 
	This is the Slave Address + R/W bit*/
	SLA_W = (0x68<<1) | 1; // the 1 means to read
	TWDR = SLA_W;

	/* Clear TWINT bit in TWCR to start transmission of address */
	TWCR = (1<<TWINT) | (1<<TWEN);

	/* Wait for TWINT Flag set
	This indicates that the SLA+W has been transmitted
	and ACK/NACK has been received. */
	while (!(TWCR & (1<<TWINT)))
		;



	// End program

	/* Transmit STOP condition */
	TWCR = (1<<TWINT)|(1<<TWEN)|
	(1<<TWSTO);

}



