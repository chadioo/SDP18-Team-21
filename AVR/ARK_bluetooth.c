#include <avr/interrupt.h>
#include <stdlib.h>
#include <string.h>
#include <avr/io.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>
#include <util/delay.h>

void initADC(){
	ADMUX = (1<<REFS0);	// set mux
	ADCSRA = (1<<ADEN)|(1<<ADPS1)|(1<<ADPS0);	// divided by prescale of 8
}

unsigned int read_adc(int axis){

	/*if(axis==0){		// z axis is PA0
		ADMUX = 0b10100000;
	}
	else if(axis==1){	// y axis is PA1
		ADMUX = 0b10100001;
	}
	else if(axis==2){	// x axis is PA2
		ADMUX = 0b10100010;
	}
	else{
		return 0;
	}*/
	axis=axis&0b00000111;
	ADMUX|=axis;
	
	ADCSRA|= (1<<ADSC);	// clear ADSC by writing one to it
	while(!(ADCSRA&(1<<ADSC)))	// wait for conversion to complete
		;
	return(ADC);		// retuens 10 bit unsigned number
}



// Method triggered by overflow
ISR(TIMER1_OVF_vect)
{
    PORTB ^= 0xFF;			//toggle PORTB values
}

// Data from Bluetooth TX given to USART RX
unsigned char USART_Receive( void )
{
	/* Wait for data to be received */
	while ( !(UCSRA & (1<<RXC)) ) {  }
	/* Get and return received data from buffer */
	return UDR;
}

// Data from USART TX given to Bluetooth RX
void USART_Transmit( unsigned char data )
{
	/* Wait for empty transmit buffer */
	while ( !( UCSRA & (1<<UDRE)) ) {  }

	/* Put data into buffer, sends the data */
	UDR = data;
}

void USART_SendString(char *str)					/* Send string of USART data function */ 
{
	int i=0;																	
	while (str[i]!=0)
	{
		USART_Transmit( str[i] );						/* Send each char of string till the NULL */
		i++;
	}
}

// Flush USART
void USART_Flush( void )
{
	unsigned char dummy;
	while ( UCSRA & (1<<RXC) ) dummy = UDR;
}

// Initialize USART
void USART_Init( unsigned int baud )
{
	/* Set baud rate */
	UBRRH = (unsigned char)(baud>>8);
	UBRRL = (unsigned char)baud;
	UCSRA |= (1<<U2X);

	/* Enable receiver and transmitter */
	UCSRB = (1<<RXEN)|(1<<TXEN);

	/* Set frame format: 8data, 2stop bit */
	UCSRC = (1<<URSEL)|(1<<USBS)|(3<<UCSZ0);
}

// 
void Bluetooth_Init(){
	//USART_Init(9600);
	USART_Init(12); // UBRR value for 9600
	char *cmd = "AT+UART=9600,2,0\r\n";
	while (*cmd != '\0'){
		USART_Transmit( *cmd );
		++cmd;
	}
}

void USART_Start_Timer()
{
	TCCR1B |= (1 << CS11);	// Timer1 prescaler = 8
	TCNT1 = 0;				// Clear the timer counter
	TIMSK = (1 << TOIE1);	// Enable timer1 overflow interrupt(TOIE1)
	sei();					// Enable global interrupts
}

// Main Method
int main(void){

	Bluetooth_Init();
	initADC();
    
	// LED Light is Port B
	DDRB = 0xFF;			// Set PORTB as output
	PORTB = 0;				// Clear PORTB bits (turn the LEDs off)
    
	// Bluetooth is Port D
	DDRD = 0x01; 			// PD0 is RX so processor reads it, PD1 is TX so processor writes it

	while(1){
	

		char DATA_IN = USART_Receive();
		
		if(DATA_IN == '0') {
			PORTB = read_adc(0);
			_delay_ms(100);
			USART_SendString( "Read Z Axis Acceleration.\n" );
			USART_SendString( read_adc(0) );
			USART_SendString( "\n" );
		}

		else if(DATA_IN == '1') {
			PORTB = read_adc(1);
			_delay_ms(100);
			USART_SendString( "Read Y Axis Acceleration.\n" );
			USART_SendString( read_adc(1) );
			USART_SendString( "\n" );
		}
		else if(DATA_IN == '2') {
			PORTB = read_adc(2);
			_delay_ms(100);
			USART_SendString( "Read X Axis Acceleration.\n" );
			USART_SendString( read_adc(2) );
			USART_SendString( "\n" );

		}
		else if(DATA_IN == '\n' || DATA_IN == '\r');
		else USART_SendString( "Please select a proper option.\n" );




/*
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

*/
		
	}

}
