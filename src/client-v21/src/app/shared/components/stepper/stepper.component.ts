import { Component, input, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkStepper } from '@angular/cdk/stepper';

@Component({
  selector: 'app-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stepper.component.html',
  styleUrl: './stepper.component.scss',
  providers: [{ provide: CdkStepper, useExisting: StepperComponent }]
})
export class StepperComponent extends CdkStepper implements OnInit {
  linearModeSelected = input(false);

  private readonly cdkStepper = inject(CdkStepper);

  ngOnInit(): void {
    this.linear = this.linearModeSelected();
  }

  onClick(index: number): void {
    this.selectedIndex = index;
  }
}
