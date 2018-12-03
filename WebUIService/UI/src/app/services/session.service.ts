import { Injectable, Output } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class SessionService {

  isLoggedIn = false;

  constructor(private _api: ApiService) { }

  getStatus() {
    return this._api.getSessionStatus().subscribe(status => {
      this.isLoggedIn = status.isLoggedIn;
    });
  }
}
