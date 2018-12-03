import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { SearchComponent } from './components/search/search.component';
import { SearchBarComponent } from './components/search-bar/search-bar.component';
import { SearchResultsComponent } from './components/search-results/search-results.component';
import { SearchResultComponent } from './components/search-result/search-result.component';
import { AutoCompleteComponent } from './components/auto-complete/auto-complete.component';

@NgModule({
  declarations: [
    AppComponent,
    SearchComponent,
    SearchBarComponent,
    SearchResultsComponent,
    SearchResultComponent,
    AutoCompleteComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
