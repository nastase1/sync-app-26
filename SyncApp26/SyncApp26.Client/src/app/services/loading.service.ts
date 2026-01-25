import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(true);

  loading$: Observable<boolean> = this.loadingSubject.asObservable();

  constructor() {
    // Loading screen shows on every refresh
  }

  finishLoading(): void {
    // Total: 500ms * 5 steps = 2500ms + 600ms circle collapse + 200ms component scale = 3300ms
    setTimeout(() => {
      this.loadingSubject.next(false);
    }, 3300);
  }

  resetLoading(): void {
    this.loadingSubject.next(true);
  }
}
