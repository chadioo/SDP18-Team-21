#include <avr/io.h>
#include <avr/interrupt.h>

ISR(TIMER1_OVF_vect)
{
    PORTB ^= 0xFF;           //toggle PORTB values 
}

int main(void)
{
    DDRB = 0xFF;	     //Set PORTB as output
    PORTB = 0;		     //Clear PORTB bits (turn the LEDs off)
    
    TCCR1B |= (1 << CS11);   //Timer1 prescaler = 8
    TCNT1 = 0;		     //Clear the timer counter
    TIMSK = (1 << TOIE1);    //Enable timer1 overflow interrupt(TOIE1)
    sei();		     //Enable global interrupts
    
    while(1);	             //Do nothing forever
	
}
