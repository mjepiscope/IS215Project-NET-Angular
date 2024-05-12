import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { ListBucketsComponent } from './component/s3/list-buckets/list-buckets.component';
import { TestComponent } from './component/test/test.component';

@NgModule({
  declarations: [
    AppComponent,
    ListBucketsComponent,
    TestComponent
  ],
  imports: [
    BrowserModule, HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
