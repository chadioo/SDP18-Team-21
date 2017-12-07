#include <avr/interrupt.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <avr/io.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>
#include <util/delay.h>


/* Initialize ADC conversion from accelerometer */
void ADC_Init()
{
	ADMUX = (1<<REFS0)|(1<<ADLAR);							// set mux
	ADCSRA = (1<<ADEN)|(1<<ADPS2)|(0<<ADPS1)|(0<<ADPS0);	// divided by prescale of 16
}


/* Read in analog XYZ data and perform ADC */
uint16_t read_adc(uint8_t axis)
{
	/* Jackie's original code, preserved for testing */
	if(axis==0){		// z axis is PA0
		axis = 0b00000000;
	}
	else if(axis==1){	// y axis is PA1
		axis = 0b00000001;
	}
	else if(axis==2){	// x axis is PA2
		axis = 0b00000010;
	}
	else{
		return 0;
	}
	//axis=axis&0b00000111;		// Choose ADC port
	ADMUX|=axis;
	
	ADCSRA|= (1<<ADSC);			// Clear ADSC by writing one to it
	while(!(ADCSRA&(1<<ADSC)))	// Wait for conversion to complete
		;
	return(ADCH);				// Returns 10 bit unsigned number
}


/* Method triggered by overflow */
ISR(TIMER1_OVF_vect)
{
    PORTB ^= 0xFF;				// toggle PORTB values
}


/* Data from Bluetooth TX given to USART RX */
unsigned char USART_Receive( void )
{
	while ( !(UCSRA & (1<<RXC)) ) {  }		// Wait for data to be received
	return UDR;								// Get and return received data from buffer
}


/* Data from USART TX given to Bluetooth RX */
void USART_Transmit( unsigned char data )
{
	while ( !( UCSRA & (1<<UDRE)) ) {  }	// Wait for empty transmit buffer
	UDR = data;								// Put data into buffer, sends the data
}


/* Send string of USART data function */
void USART_SendString(char *str)
{
	int i=0;																	
	while (str[i]!=0)
	{
		USART_Transmit( str[i] );	// Send each char of string till the NULL
		i++;
	}
}


/* Flush USART */
void USART_Flush( void )
{
	unsigned char dummy;
	while ( UCSRA & (1<<RXC) ) dummy = UDR;
}


/* Initialize USART */
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


/* Initialize the BT module */
void Bluetooth_Init()
{
	USART_Init(12); 	// ~UBRR value for 9600 baud
	char *cmd = "Bluetooth Init: AT+UART=9600,2,0\r\n";
	while (*cmd != '\0')
	{
		USART_Transmit( *cmd );
		++cmd;
	}
}


/* USART timer, not used for now */
void USART_Start_Timer()
{
	TCCR1B |= (1 << CS11);	// Timer1 prescaler = 8
	TCNT1 = 0;				// Clear the timer counter
	TIMSK = (1 << TOIE1);	// Enable timer1 overflow interrupt(TOIE1)
	sei();					// Enable global interrupts
}


/* Recursive method for sending ADC */
void Start_ADC(char ch)
{
	if(ch == '0' || ch == '1' || ch == '2')
	{
		Send_ADC(ch);
	}
	else
	{
		if(ch == '\n' || ch == '\r');
		else USART_SendString( "Please select a proper option.\n" );
		ch = USART_Receive();
		Start_ADC(ch);
	}
}


/* Actually sends the ADC */
void Send_ADC(char ch)
{
	char analogData = 0;
	if(ch == '0')
	{
		while(ch != '1' && ch != '2')
		{
			USART_Flush();
			analogData = read_adc(0);
			PORTB = analogData;
//			USART_SendString( "Read X Axis Acceleration.\n" );
			while((ADCSRA&(1<<ADSC)));
			USART_Transmit( analogData );
			USART_SendString( "\n" );
			_delay_ms(100);					// This can go down to 2ms, leaving up though for testing
			if( UCSRA & (1<<UDRE) )
			{
				ch = UDR;
			}
			_delay_ms(100);
		}
		Start_ADC(ch);
	}

	else if(ch == '1')
	{
		while(ch != '0' && ch != '2')
		{
			USART_Flush();
			analogData = read_adc(1);
			PORTB = analogData;
//			USART_SendString( "Read Y Axis Acceleration.\n" );
			while((ADCSRA&(1<<ADSC)));
			USART_Transmit( analogData );
			USART_SendString( "\n" );
			_delay_ms(100);					// This can go down to 2ms, leaving up though for testing
			if( UCSRA & (1<<UDRE) )
			{
				ch = UDR;
			}
			_delay_ms(100);
		}
		Start_ADC(ch);
	}

	else if(ch == '2')
	{
		while(ch != '0' && ch != '1')
		{
			USART_Flush();
			analogData = read_adc(2);
			PORTB = analogData;
//			USART_SendString( "Read Z Axis Acceleration.\n" );
			while((ADCSRA&(1<<ADSC)));
			USART_Transmit( analogData );
			USART_SendString( "\n" );
			_delay_ms(100);					// This can go down to 2ms, leaving up though for testing
			if( UCSRA & (1<<UDRE) )
			{
				ch = UDR;
			}
			_delay_ms(100);
		}
		Start_ADC(ch);
	}
}


/* Main Method */
int main(void)
{
	Bluetooth_Init();
	ADC_Init();

	char DATA_IN = USART_Receive();
    
	/* LED Light is Port B, right now for testing purposes */
	DDRB = 0xFF;			// Set PORTB as output
	PORTB = 0;				// Clear PORTB bits (turn the LEDs off)
    
	/* Bluetooth is Port D */
	DDRD = 0x01; 			// PD0 is RX so processor reads it, PD1 is TX so processor writes it

	Start_ADC(DATA_IN);		// Recursive ADC function
/////////////////////////////////////////////////////////////////
/*	while(1)
	{
		DATA_IN = USART_Receive();		// Data in from phone
		USART_Flush();

		if(DATA_IN == '0')
		{
			while(DATA_IN != '1' && DATA_IN != '2')
			{
				analogData = read_adc(0);
				PORTB = analogData;
//				USART_SendString( "Read X Axis Acceleration.\n" );
				while((ADCSRA&(1<<ADSC)));
				USART_Transmit( analogData );
				USART_SendString( "\n" );
				_delay_ms(50);					// This can go down to 2ms, leaving up though for testing
				if( UCSRA & (1<<UDRE) )
				{
					DATA_IN = UDR;
				}
			}
			USART_Flush();
			continue;
		}

		if(DATA_IN == '1')
		{
			while(DATA_IN != '0' && DATA_IN != '2')
			{
				analogData = read_adc(1);
				PORTB = analogData;
//				USART_SendString( "Read Y Axis Acceleration.\n" );
				while((ADCSRA&(1<<ADSC)));
				USART_Transmit( analogData );
				USART_SendString( "\n" );
				_delay_ms(50);					// This can go down to 2ms, leaving up though for testing
				if( UCSRA & (1<<UDRE) )
				{
					DATA_IN = UDR;
				}
			}
			USART_Flush();
			continue;
		}

		if(DATA_IN == '2')
		{
			while(DATA_IN != '0' && DATA_IN != '1')
			{
				analogData = read_adc(2);
				PORTB = analogData;
//				USART_SendString( "Read Z Axis Acceleration.\n" );
				while((ADCSRA&(1<<ADSC)));
				USART_Transmit( analogData );
				USART_SendString( "\n" );
				_delay_ms(50);					// This can go down to 2ms, leaving up though for testing
				if( UCSRA & (1<<UDRE) )
				{
					DATA_IN = UDR;
				}
			}
			USART_Flush();
			continue;
		}

//		else if(DATA_IN == '\n' || DATA_IN == '\r');
//		else USART_SendString( "Please select a proper option.\n" );
	}
*/
}
