import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-screen',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loading-screen.component.html',
  styleUrls: ['./loading-screen.component.css']
})
export class LoadingScreenComponent implements OnInit {
  loadingProgress = 0;
  loadingText = 'Inițializare...';
  isCollapsing = false;
  ngOnInit(): void {
    this.simulateLoading();
  }

  private simulateLoading(): void {
    const steps = [
      { progress: 20, text: 'Se încarcă resursele...' },
      { progress: 40, text: 'Se conectează la server...' },
      { progress: 60, text: 'Se sincronizează datele...' },
      { progress: 80, text: 'Se pregătește interfața...' },
      { progress: 100, text: 'Gata!' }
    ];

    let currentStep = 0;
    const interval = setInterval(() => {
      if (currentStep < steps.length) {
        this.loadingProgress = steps[currentStep].progress;
        this.loadingText = steps[currentStep].text;
        
        // Start collapse animation at 90%
        if (this.loadingProgress >= 90) {
          this.isCollapsing = true;
        }
        
        currentStep++;
      } else {
        clearInterval(interval);
      }
    }, 500);
  }
}
