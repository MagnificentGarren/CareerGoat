import { DOCUMENT } from '@angular/common';
import { AfterViewInit, Component, ElementRef, inject, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

interface Particle {
  x: number;
  y: number;
  radius: number;
  speed: number;
  drift: number;
  phase: number;
  gold: boolean;
}

@Component({
  selector: 'app-landing-page',
  imports: [RouterLink],
  templateUrl: './landing-page.html',
})
export class LandingPage implements AfterViewInit, OnDestroy, OnInit {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly document = inject(DOCUMENT);
  private hero: HTMLElement | null = null;
  private art: SVGSVGElement | null = null;
  private canvas: HTMLCanvasElement | null = null;
  private context: CanvasRenderingContext2D | null = null;
  private reduceMotion: MediaQueryList | null = null;
  private particles: Particle[] = [];
  private frame = 0;
  private width = 0;
  private height = 0;
  private pointer = { x: 0, y: 0, active: false };
  private revealObserver: IntersectionObserver | null = null;

  private readonly resizeCanvas = (): void => {
    if (!this.hero || !this.canvas || !this.context) return;
    const bounds = this.hero.getBoundingClientRect();
    const pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
    this.width = bounds.width;
    this.height = bounds.height;
    this.canvas.width = Math.round(this.width * pixelRatio);
    this.canvas.height = Math.round(this.height * pixelRatio);
    this.context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
    this.particles = Array.from({ length: Math.min(105, Math.round(this.width / 9)) }, () => ({
      x: Math.random() * this.width,
      y: Math.random() * this.height,
      radius: 0.45 + Math.random() * 1.55,
      speed: 0.12 + Math.random() * 0.43,
      drift: (Math.random() - 0.5) * 0.22,
      phase: Math.random() * Math.PI * 2,
      gold: Math.random() > 0.75,
    }));
    this.drawParticles();
  };

  private readonly drawParticles = (): void => {
    if (!this.context || !this.reduceMotion) return;
    const context = this.context;
    context.clearRect(0, 0, this.width, this.height);
    this.particles.forEach((particle) => {
      if (!this.reduceMotion?.matches) {
        particle.y -= particle.speed;
        particle.x += particle.drift;
        particle.phase += 0.018;
        if (particle.y < -5) {
          particle.y = this.height + 5;
          particle.x = Math.random() * this.width;
        }
        if (particle.x < -5) particle.x = this.width + 5;
        if (particle.x > this.width + 5) particle.x = -5;
      }

      const dx = this.pointer.x - particle.x;
      const dy = this.pointer.y - particle.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      const nearby = this.pointer.active && distance < 110;
      const shimmer = 0.28 + (Math.sin(particle.phase) + 1) * 0.17;
      const color = particle.gold ? '255,212,71' : '98,229,244';
      context.beginPath();
      context.arc(
        particle.x,
        particle.y,
        nearby ? particle.radius * 1.65 : particle.radius,
        0,
        Math.PI * 2,
      );
      context.fillStyle = `rgba(${color},${nearby ? 0.95 : shimmer})`;
      context.shadowBlur = nearby ? 13 : 0;
      context.shadowColor = `rgba(${color},.8)`;
      context.fill();
      context.shadowBlur = 0;

      if (nearby) {
        context.beginPath();
        context.moveTo(particle.x, particle.y);
        context.lineTo(this.pointer.x, this.pointer.y);
        context.strokeStyle = `rgba(${color},${0.24 * (1 - distance / 110)})`;
        context.lineWidth = 0.7;
        context.stroke();
      }
    });

    if (!this.reduceMotion.matches) this.frame = requestAnimationFrame(this.drawParticles);
  };

  private readonly onPointerMove = (event: PointerEvent): void => {
    if (!this.hero || !this.art) return;
    const bounds = this.hero.getBoundingClientRect();
    this.pointer.x = event.clientX - bounds.left;
    this.pointer.y = event.clientY - bounds.top;
    this.pointer.active = true;
    this.hero.style.setProperty('--spot-x', `${this.pointer.x}px`);
    this.hero.style.setProperty('--spot-y', `${this.pointer.y}px`);
    this.art.style.setProperty('--art-x', `${(this.pointer.x / bounds.width - 0.5) * -10}px`);
    this.art.style.setProperty('--art-y', `${(this.pointer.y / bounds.height - 0.5) * -8}px`);
  };

  private readonly onPointerLeave = (): void => {
    this.pointer.active = false;
    this.art?.style.setProperty('--art-x', '0px');
    this.art?.style.setProperty('--art-y', '0px');
  };

  private readonly onMotionChange = (): void => {
    cancelAnimationFrame(this.frame);
    this.drawParticles();
  };

  ngOnInit(): void {
    this.document.body.classList.add('site-page');
  }

  ngAfterViewInit(): void {
    this.hero = this.element.nativeElement.querySelector('.hero');
    this.art = this.element.nativeElement.querySelector('.art-scene');
    this.canvas = this.element.nativeElement.querySelector('#particle-field');
    this.context = this.canvas?.getContext('2d') ?? null;
    this.reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

    if (this.hero && this.canvas && this.context) {
      this.hero.addEventListener('pointermove', this.onPointerMove);
      this.hero.addEventListener('pointerleave', this.onPointerLeave);
      window.addEventListener('resize', this.resizeCanvas, { passive: true });
      this.reduceMotion.addEventListener('change', this.onMotionChange);
      this.resizeCanvas();
    }

    const revealItems = this.element.nativeElement.querySelectorAll<HTMLElement>('.reveal');
    if ('IntersectionObserver' in window && !this.reduceMotion.matches) {
      this.revealObserver = new IntersectionObserver(
        (entries, observer) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              entry.target.classList.add('in-view');
              observer.unobserve(entry.target);
            }
          });
        },
        { threshold: 0.18 },
      );
      revealItems.forEach((item) => this.revealObserver?.observe(item));
    } else {
      revealItems.forEach((item) => item.classList.add('in-view'));
    }
  }

  ngOnDestroy(): void {
    this.document.body.classList.remove('site-page');
    this.hero?.removeEventListener('pointermove', this.onPointerMove);
    this.hero?.removeEventListener('pointerleave', this.onPointerLeave);
    window.removeEventListener('resize', this.resizeCanvas);
    this.reduceMotion?.removeEventListener('change', this.onMotionChange);
    this.revealObserver?.disconnect();
    cancelAnimationFrame(this.frame);
  }
}
