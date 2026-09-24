import { Component, computed, input } from '@angular/core';
/** Shows how long until an interview: "in 3 days", "Tomorrow", "Today", "Passed". */
@Component({
selector: 'app-countdown',
template: `{{ label() }}`,
styleUrl: './countdown.scss',
host: {
class: 'countdown',
'[class.countdown--urgent]': 'days() !== null && days()! >= 0 && days()! <= 2',
'[class.countdown--past]': 'days() !== null && days()! < 0',
},
})
export class Countdown {
/** Whole days until the interview; negative once it has passed. */
readonly days = input.required<number | null>();
protected readonly label = computed(() => {
const days = this.days();
if (days === null) {
return 'No date set';
}
if (days < 0) {
return 'Interview passed';
}
if (days === 0) {
return 'Today';
}
if (days === 1) {
return 'Tomorrow';
}
return `In ${days} days`;
});
}