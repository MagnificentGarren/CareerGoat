import { DOCUMENT } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

export interface InformationSection {
  heading: string;
  body: string;
  status?: string;
  points?: string[];
}

export interface InformationContent {
  eyebrow: string;
  title: string;
  intro: string;
  sections: InformationSection[];
}

@Component({
  selector: 'app-information-page',
  imports: [RouterLink],
  templateUrl: './information-page.html',
})
export class InformationPage implements OnInit, OnDestroy {
  private readonly document = inject(DOCUMENT);
  private readonly route = inject(ActivatedRoute);
  private routeSubscription?: Subscription;
  content: InformationContent = {
    eyebrow: '',
    title: '',
    intro: '',
    sections: [],
  };

  ngOnInit(): void {
    this.document.body.classList.add('site-page');
    this.routeSubscription = this.route.data.subscribe((data) => {
      this.content = data['content'] as InformationContent;
    });
  }

  ngOnDestroy(): void {
    this.document.body.classList.remove('site-page');
    this.routeSubscription?.unsubscribe();
  }
}
