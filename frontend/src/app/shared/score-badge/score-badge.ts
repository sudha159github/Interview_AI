import { Component, computed, input } from '@angular/core';

/** Match score pill: green (80+), amber (60-79), red (below 60). */
@Component({
  selector: 'app-score-badge',
  template: `{{ score() }}%`,
  styleUrl: './score-badge.scss',
  host: {
    class: 'score-badge',
    '[class.score-badge--high]': "level() === 'high'",
    '[class.score-badge--mid]': "level() === 'mid'",
    '[class.score-badge--low]': "level() === 'low'",
    '[class.score-badge--large]': "size() === 'large'",
    '[attr.aria-label]': "'Match score ' + score() + ' percent'",
  },
})
export class ScoreBadge {
  readonly score = input.required<number>();
  readonly size = input<'normal' | 'large'>('normal');

  protected readonly level = computed(() => {
    const score = this.score();
    return score >= 80 ? 'high' : score >= 60 ? 'mid' : 'low';
  });
}