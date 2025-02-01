shader_type canvas_item;
//
//void vertex() {
	//// Called for every vertex the material is visible on.
//}
//below will be uniform[61] eventually
//const vec2 purple_slimes[3] = {vec2(0.5,0.5), vec2(0.6,0.6), vec2(0.2,0.1)};
//const int num_purple = 3;


uniform sampler2D noise : repeat_enable;

const vec2 screen = vec2(float(1280/3),float(720/3));

uniform vec2 green_slimes[61];
uniform int num_green;
uniform vec2 purple_slimes[61];
uniform int num_purple;

const float scroll_speed = 0.1;
const float blob_radius = 0.025;
const float bubble_multiplier = 8.0;

const vec4 green_color = vec4(0.0,0.5,0.0,0.5);
const vec4 purple_color = vec4(0.3,0.0,0.3,0.5);

void fragment() {
	vec2 pixel_uv = floor(UV*screen)/screen;
	// Called for every pixel the material is visible on.
	COLOR = vec4(0.0,0.0,0.0,0.0);
	float distance_count=0.0;
	float noise_raw = texture(noise, pixel_uv+TIME*scroll_speed).x;
	float noise_col = (noise_raw-0.5)*bubble_multiplier;
	distance_count+=noise_col;
	for(int i=0; i<num_green; i++){
		vec2 this_dist = pow(pixel_uv-green_slimes[i],vec2(2,2));
		distance_count += inversesqrt(this_dist.x + this_dist.y);
	}
	
	for(int i=0; i<num_purple; i++){
		vec2 this_dist = pow(pixel_uv-purple_slimes[i],vec2(2,2));
		distance_count -= inversesqrt(this_dist.x + this_dist.y);
	}
	if(distance_count>(1.0/blob_radius)){
		COLOR=green_color;
	}
	if(distance_count<(-1.0/blob_radius)){
		COLOR=purple_color;
	}
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
