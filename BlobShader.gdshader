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
uniform int current_player;

const vec2 offset = vec2(0.0,-0.03);
const float blob_radius = 0.035; //as a percent of screen width
const float blob_exponent = 2.5;
const float blob_threshold = pow(blob_radius,-blob_exponent);

const float spec_internal_radius = 0.75;
const float spec_external_radius = 0.90;
const float shadow_radius = 1.4;
const float spec_internal_threshold = pow(blob_radius*spec_internal_radius,-blob_exponent);
const float spec_external_threshold = pow(blob_radius*spec_external_radius,-blob_exponent);
const float shadow_threshold = pow(blob_radius*shadow_radius, -blob_exponent);
const float spec_cos_threshold = 0.9;
const float shadow_cos_threshold = 0.7;

uniform vec2 ball_position;

const vec2 light_direction = vec2(-sqrt(0.5),sqrt(0.5));

const float scroll_speed = 0.05;
const float bubble_multiplier = 0.35;

uniform float progress;
uniform vec2 move_position;
uniform int move_type;

const vec4 green_color = vec4(0.0,0.5,0.0,0.3);
const vec4 purple_color = vec4(0.3,0.0,0.3,0.3);
const vec4 spec_green_color = vec4(0.7,1.0,0.7,0.7);
const vec4 spec_purple_color = vec4(1.0,0.5,1.0,0.7);
const vec4 shadow_color = vec4(0.0,0.0,0.0,0.5);

void fragment() {
	//vec2 pixel_uv = floor(UV*screen)/screen;
	vec2 pixel_uv = UV;
	// Called for every pixel the material is visible on.
	COLOR = vec4(0.0,0.0,0.0,0.0);
	float distance_count=0.0;
	float noise_raw = texture(noise, pixel_uv+TIME*scroll_speed).x+texture(noise, pixel_uv-TIME*scroll_speed).y;
	float noise_col = (noise_raw-1.0)*bubble_multiplier*blob_threshold;
	distance_count+=noise_col;
	
	for(int i=0; i<num_green; i++){
		vec2 sq_dist = pow(pixel_uv-green_slimes[i]-offset,vec2(2,2));
		float this_dist = sqrt(sq_dist.x + sq_dist.y);
		distance_count+=pow(this_dist,-blob_exponent);
	}
	
	for(int i=0; i<num_purple; i++){
		vec2 sq_dist = pow(pixel_uv-purple_slimes[i]-offset,vec2(2,2));
		float this_dist = sqrt(sq_dist.x + sq_dist.y);
		distance_count-=pow(this_dist,-blob_exponent);
	}
	
	float player_mult = float(current_player)*2.0 - 1.0;
	
	if(move_type==0){
		vec2 sq_dist = pow(pixel_uv-move_position-offset,vec2(2,2));
		float this_dist = sqrt(sq_dist.x + sq_dist.y);
		distance_count+=pow(this_dist,-blob_exponent)*player_mult*(1.0-progress);
	}
		if(move_type==1){
		vec2 sq_dist = pow(pixel_uv-move_position-offset,vec2(2,2));
		float this_dist = sqrt(sq_dist.x + sq_dist.y);
		distance_count-=pow(this_dist,-blob_exponent)*player_mult*(1.0-progress);
	}
	
	//vec2 sq_dist = pow(pixel_uv-ball_position,vec2(2,2));
	//float this_dist = sqrt(sq_dist.x + sq_dist.y);
	//float ball_mult;
	//if(this_dist<=blob_radius){
		//ball_mult = (this_dist)/(blob_radius);
	//} else {
		//ball_mult = 1.0;
	//}
	//distance_count *= ball_mult;
	
	vec2 local_gradient = vec2(dFdx(distance_count),dFdy(distance_count));
	float local_grad_norm = sqrt(pow(local_gradient.x,2)+pow(local_gradient.y,2));
	vec2 local_normal = local_gradient/local_grad_norm;
	float light_parallel = dot(local_normal, light_direction);
	
	float generalized_distance = sign(distance_count)*pow(abs(distance_count), -1.0/blob_exponent);
	float thickness;
	if(abs(generalized_distance)<blob_radius){
		thickness = 1.0-abs(generalized_distance)/blob_radius;
	}else{
		thickness=0.0;
	}
	
	//color in purple blobs
	if(distance_count>(blob_threshold)){
		COLOR=green_color + vec4(-0.2,-0.2,-0.2,0.4)*thickness;
		//color in purple specular
		if(
			light_parallel>spec_cos_threshold &&
			abs(distance_count)<spec_internal_threshold &&
			abs(distance_count)>spec_external_threshold
		){
			COLOR = spec_green_color;
		}
	}
	//color in purple blobs
	if(distance_count<(-blob_threshold)){
		COLOR=purple_color+ vec4(-0.2,-0.2,-0.2,0.4)*thickness;
		//color in purple speculars
		if(
			light_parallel<-spec_cos_threshold &&
			abs(distance_count)<spec_internal_threshold &&
			abs(distance_count)>spec_external_threshold
		){
			COLOR = spec_purple_color;
		}
	}
	
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
