﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	// configurables
	public float speed = 7f;
	public float acceleration = 0.75f;
	public float jump = 10f;
	public float inputBuffer = 0.05f;
	public bool canDoubleJump = true;
	public bool mirrorWhenTurning = true;

	// physics
	private Rigidbody2D body;
	public Transform groundCheck;
	public LayerMask groundLayer;
	public float groundCheckRadius = 0.2f;
	public float wallCheckDistance = 1f;
	public bool checkForEdges = false;
	private float groundAngle = 0;

	// flags
	private bool canControl = true;
	private bool running = false;
	private bool grounded = false;
	private bool doubleJumped = false;

	// misc
	private float jumpBufferedFor = 0;

	// particles
	public GameObject jumpParticles, landParticles;

	// sound stuff
	private AudioSource audioSource;
	public AudioClip jumpClip, landClip;

	// animations
	private Animator anim;

	// ###############################################################

	// Use this for initialization
	void Start () {
		body = GetComponent<Rigidbody2D> ();
		audioSource = GetComponent<AudioSource> ();
		anim = GetComponentInChildren<Animator> ();
	}
	
	// Update is called once per frame
	void Update () {

		bool wasGrounded = grounded;

		if (!checkForEdges) {
			grounded = Physics2D.OverlapCircle (groundCheck.position, groundCheckRadius, groundLayer);

			// draw debug lines
			Color debugLineColor = grounded ? Color.green : Color.red;
			Debug.DrawLine (transform.position, groundCheck.position, debugLineColor, 0.2f);
			Debug.DrawLine (groundCheck.position + Vector3.left * groundCheckRadius, groundCheck.position + Vector3.right * groundCheckRadius, debugLineColor, 0.2f);
		} else {
			grounded = Physics2D.Raycast (transform.position, Vector2.down, 1f);

			// draw debug lines
			Color debugLineColor = grounded ? Color.green : Color.red;
			Debug.DrawRay (transform.position, Vector2.down, debugLineColor, 0.2f);
		}

		// just landed
		if (!wasGrounded && grounded) {
			Land ();
		}

		// just left the ground
		if (wasGrounded && !grounded) {
			groundAngle = 0;
		}

		// jump buffer timing
		if (jumpBufferedFor > 0) {
			jumpBufferedFor -= Time.deltaTime;
		}

		// controls
		if (canControl) {

			float inputDirection = Input.GetAxis("Horizontal");

			// jump
			if ((grounded || (canDoubleJump && !doubleJumped)) && (Input.GetButtonDown("Jump") || jumpBufferedFor > 0)) {

				body.velocity = new Vector2 (body.velocity.x, 0); // reset vertical speed

				if (!grounded) {
					doubleJumped = true;
				}

				jumpBufferedFor = 0;

				// jump sounds
				if (audioSource && jumpClip) {
					audioSource.PlayOneShot (jumpClip);
				}

				// jump particles
				if (jumpParticles) {
					Instantiate (jumpParticles, groundCheck.position, Quaternion.identity);
				}

				// animation
				if (anim) {
					anim.speed = 1f;
					anim.SetTrigger ("jump");
					anim.ResetTrigger ("land");
				}

				body.AddForce (Vector2.up * jump, ForceMode2D.Impulse);

			} else if (canControl && Input.GetButtonDown("Jump")) {
			
				// jump command buffering
				jumpBufferedFor = 0.2f;
			}

			// moving
			Vector2 moveVector = new Vector2 (speed * inputDirection, body.velocity.y);

			if (Mathf.Sign (body.velocity.x) == Mathf.Sign (moveVector.x)) {
				body.velocity = Vector2.MoveTowards (body.velocity, moveVector, acceleration);
			} else {
				body.velocity = moveVector;
			}

			// direction
			if (mirrorWhenTurning && Mathf.Abs(inputDirection) > inputBuffer) {

				float dir = Mathf.Sign (inputDirection);
				transform.localScale = new Vector2 (dir, 1);

//				Transform sprite = transform.Find("Character");
//				Vector3 scl = sprite.localScale;
//				scl.x = dir;
//				sprite.localScale = scl;

//				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 90f - dir * 90f, transform.localEulerAngles.z);
			}

			Vector2 p = transform.position + Vector3.right * inputDirection * wallCheckDistance;
			bool wallHug = Physics2D.OverlapCircle (p, groundCheckRadius, groundLayer);
			Color hugLineColor = grounded ? Color.green : Color.red;
			Debug.DrawLine (transform.position, p, hugLineColor, 0.2f);

			running = inputDirection < -inputBuffer || inputDirection > inputBuffer;

			if (wallHug && !checkForEdges) {
				body.velocity = new Vector2 (0, body.velocity.y);
				running = false;
			}

			if (!grounded) {
				running = false; 
			}

			if (anim) {

				anim.SetBool ("running", running);

				if (running) {
					anim.speed = Mathf.Abs (body.velocity.x * 0.18f);
					anim.SetFloat ("speed", Mathf.Abs(body.velocity.x));
				} else {
					anim.speed = 1f;
					anim.SetFloat ("speed", 0);
				}
			}
		}
	}

	private void Land() {

		doubleJumped = false;

		// landing sound
		if (audioSource && landClip) {
			audioSource.PlayOneShot (landClip);
		}

		// landing particles
		if (landParticles) {
			Instantiate (landParticles, groundCheck.position, Quaternion.identity);
		}

		// animation
		if (anim) {
			anim.speed = 1f;
			anim.SetTrigger ("land");
		}
	}

	public bool IsGrounded() {
		return grounded;
	}

	void OnCollisionStay2D(Collision2D coll) {
		groundAngle = Mathf.Atan2(coll.contacts [0].normal.y, coll.contacts [0].normal.x) * Mathf.Rad2Deg - 90;
	}

	void OnCollisionEnter2D(Collision2D coll) {
		groundAngle = Mathf.Atan2(coll.contacts [0].normal.y, coll.contacts [0].normal.x) * Mathf.Rad2Deg - 90;
	}

	public float GetGroundAngle() {
		if (Mathf.Abs (groundAngle) > 90) {
			groundAngle = 0;
		}
		return groundAngle;
	}
}