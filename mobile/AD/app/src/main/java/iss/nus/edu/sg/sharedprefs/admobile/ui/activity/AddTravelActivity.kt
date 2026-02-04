package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.RecyclerView
import androidx.viewpager2.widget.CompositePageTransformer
import androidx.viewpager2.widget.ViewPager2
import com.airbnb.lottie.LottieAnimationView
import com.google.android.gms.common.api.Status
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.OnMapReadyCallback
import com.google.android.gms.maps.SupportMapFragment
import com.google.android.gms.maps.model.*
import com.google.android.libraries.places.api.Places
import com.google.android.libraries.places.api.model.Place
import com.google.android.libraries.places.widget.AutocompleteSupportFragment
import com.google.android.libraries.places.widget.listener.PlaceSelectionListener
import com.google.android.libraries.places.widget.model.AutocompleteActivityMode
import com.google.android.material.appbar.MaterialToolbar
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddTravelRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TransportMode
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import kotlinx.coroutines.launch

class AddTravelActivity : AppCompatActivity(), OnMapReadyCallback {

    private lateinit var transportViewPager: ViewPager2
    private lateinit var selectedModeEditText: EditText
    private lateinit var emissionFactorText: TextView
    private lateinit var routeInfoText: TextView
    private lateinit var notesEditText: EditText

    private lateinit var originFragment: AutocompleteSupportFragment
    private lateinit var destFragment: AutocompleteSupportFragment

    private lateinit var mMap: GoogleMap
    private var originAddress = ""
    private var destinationAddress = ""
    private var currentModeId = 6

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_travel_activity)

        if (!Places.isInitialized()) {
            Places.initialize(applicationContext, "AIzaSyCM7mWNDAl1ZzJ5Kw2eCdSpBwq_gqyNPr0")
        }

        setupSearch()
        initUI()
        setupViewPager()
    }

    private fun initUI() {
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        transportViewPager = findViewById(R.id.transport_view_pager)
        selectedModeEditText = findViewById(R.id.selected_transport_edittext)
        emissionFactorText = findViewById(R.id.emission_factor_text)
        routeInfoText = findViewById(R.id.route_info_text)
        notesEditText = findViewById(R.id.notes_edittext)

        val mapFragment = supportFragmentManager.findFragmentById(R.id.map) as SupportMapFragment
        mapFragment.getMapAsync(this)

        findViewById<Button>(R.id.save_button).setOnClickListener { submitData() }
        findViewById<ImageView>(R.id.iv_swap_locations)?.setOnClickListener { swapLocations() }
    }

    private fun setupSearch() {
        originFragment = supportFragmentManager.findFragmentById(R.id.origin_autocomplete_fragment) as AutocompleteSupportFragment
        destFragment = supportFragmentManager.findFragmentById(R.id.destination_autocomplete_fragment) as AutocompleteSupportFragment

        configureAutocomplete(originFragment, "Search Origin...", true)
        configureAutocomplete(destFragment, "Search Destination...", false)

        listOf(originFragment, destFragment).forEach { fragment ->
            fragment.view?.let { view ->
                view.findViewById<View>(com.google.android.libraries.places.R.id.places_autocomplete_search_button)?.visibility = View.GONE
                val editText = view.findViewById<EditText>(com.google.android.libraries.places.R.id.places_autocomplete_search_input)
                editText.background = null
                editText.setPadding(0, editText.paddingTop, 0, editText.paddingBottom)
                editText.textSize = 15f
            }
        }
    }

    private fun configureAutocomplete(fragment: AutocompleteSupportFragment, hint: String, isOrigin: Boolean) {
        fragment.setPlaceFields(listOf(Place.Field.ID, Place.Field.NAME, Place.Field.LAT_LNG, Place.Field.ADDRESS))
            .setHint(hint)
            .setCountries("SG")
            .setActivityMode(AutocompleteActivityMode.OVERLAY)

        fragment.setOnPlaceSelectedListener(object : PlaceSelectionListener {
            override fun onPlaceSelected(place: Place) {
                val address = place.address ?: place.name ?: ""
                val latLng = place.latLng
                if (isOrigin) {
                    originAddress = address
                    // 🌟 修改：如果是全球模式，缩放稍微远一点
                    val zoomLevel = if (currentModeId == 9 || currentModeId == 5) 10f else 14f
                    latLng?.let { mMap.animateCamera(CameraUpdateFactory.newLatLngZoom(it, zoomLevel)) }
                } else {
                    destinationAddress = address
                    val zoomLevel = if (currentModeId == 9 || currentModeId == 5) 10f else 14f
                    latLng?.let { mMap.animateCamera(CameraUpdateFactory.newLatLngZoom(it, zoomLevel)) }
                }
                updateRouteInfoSimple()
            }
            override fun onError(status: Status) { Log.e("Places", "Error: $status") }
        })
    }

    // 🌟 核心功能：动态更新搜索范围和地图视野
    private fun updateAutocompleteConstraints(isGlobal: Boolean) {
        val countries = if (isGlobal) emptyList() else listOf("SG")
        originFragment.setCountries(countries)
        destFragment.setCountries(countries)

        if (::mMap.isInitialized) {
            if (isGlobal) {
                // 如果是飞机/船，地图视角拉远到全球
                mMap.animateCamera(CameraUpdateFactory.newLatLngZoom(LatLng(1.3521, 103.8198), 1f))
            } else {
                // 如果是市内交通，聚焦新加坡
                mMap.animateCamera(CameraUpdateFactory.newLatLngZoom(LatLng(1.3521, 103.8198), 11f))
            }
        }
    }

    private fun swapLocations() {
        if (!::originFragment.isInitialized || !::destFragment.isInitialized) return
        if (originAddress.isEmpty() && destinationAddress.isEmpty()) return

        val tempAddress = originAddress
        originAddress = destinationAddress
        destinationAddress = tempAddress

        originFragment.setText(originAddress)
        destFragment.setText(destinationAddress)
        updateRouteInfoSimple()

        Toast.makeText(this, "Locations swapped", Toast.LENGTH_SHORT).show()
    }

    private fun updateRouteInfoSimple() {
        routeInfoText.text = "From: $originAddress\nTo: $destinationAddress"
    }

    private fun setupViewPager() {
        val transportModes = listOf(
            TransportMode(6, "Car (Gasoline)", R.raw.transport_sedan, 0.2),
            TransportMode(7, "Car (Electric)", R.raw.transport_sedan2, 0.05),
            TransportMode(4, "Bus", R.raw.transport_bus, 0.05),
            TransportMode(2, "Motorcycle", R.raw.transport_cycling, 0.02),
            TransportMode(3, "Subway", R.raw.transport_train, 0.03),
            TransportMode(5, "Ship", R.raw.transport_ship, 0.03),
            TransportMode(9, "Airplane", R.raw.transport_airplane, 0.25)
        )

        transportViewPager.adapter = TransportModeAdapter(transportModes)
        transportViewPager.orientation = ViewPager2.ORIENTATION_VERTICAL
        transportViewPager.clipToPadding = false
        transportViewPager.clipChildren = false
        transportViewPager.offscreenPageLimit = 1

        val compositePageTransformer = CompositePageTransformer()
        compositePageTransformer.addTransformer { page, position ->
            val absPos = Math.abs(position)
            if (absPos <= 1.0f) {
                page.alpha = 0.5f + (1 - absPos) * 0.5f
                val scale = 0.9f + (1 - absPos) * 0.1f
                page.scaleX = scale
                page.scaleY = scale
                val peekHeight = 45 * resources.displayMetrics.density
                page.translationY = -page.height * position + (peekHeight * position)
                page.elevation = (1 - absPos) * 10f
            } else {
                page.alpha = 0f
            }
        }
        transportViewPager.setPageTransformer(compositePageTransformer)

        transportViewPager.registerOnPageChangeCallback(object : ViewPager2.OnPageChangeCallback() {
            override fun onPageSelected(position: Int) {
                val mode = transportModes[position]
                currentModeId = mode.id

                emissionFactorText.text = "Emission Factor: ${mode.emissionFactor} kgCO2e/km"
                selectedModeEditText.setText(mode.name)

                // 🌟 修改：当选中 飞机(9) 或 轮船(5) 时，启用全球搜索
                val isGlobalMode = (currentModeId == 9 || currentModeId == 5)
                updateAutocompleteConstraints(isGlobalMode)
            }
        })
    }

    override fun onMapReady(googleMap: GoogleMap) {
        mMap = googleMap
        mMap.moveCamera(CameraUpdateFactory.newLatLngZoom(LatLng(1.3521, 103.8198), 11f))
    }


    private fun submitData() {
        if (originAddress.isEmpty() || destinationAddress.isEmpty()) {
            Toast.makeText(this, "Please select both points", Toast.LENGTH_SHORT).show()
            return
        }

        val request = AddTravelRequest(
            originAddress = originAddress,
            destinationAddress = destinationAddress,
            transportMode = currentModeId,
            notes = notesEditText.text.toString()
        )

        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = prefs.getString("access_token", "") ?: ""

                if (token.isEmpty()) {
                    Toast.makeText(this@AddTravelActivity, "Token missing, please re-login", Toast.LENGTH_SHORT).show()
                    return@launch
                }

                val fullToken = "Bearer $token"

                val response = NetworkClient.apiService.addTravelRecord(fullToken, request)

                if (response.isSuccessful) {
                    val body = response.body()
                    Log.d("TRAVEL_DEBUG", "Saved: ${body?.distanceKilometers} km, ${body?.carbonEmission} kg")

                    Toast.makeText(this@AddTravelActivity,
                        "Saved! ${body?.distanceKilometers}km recorded",
                        Toast.LENGTH_LONG).show()

                    finish()
                } else {
                    Log.e("TRAVEL_DEBUG", "Error Code: ${response.code()} Body: ${response.errorBody()?.string()}")
                    Toast.makeText(this@AddTravelActivity, "Server Error: ${response.code()}", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Log.e("TRAVEL_DEBUG", "Network Error: ${e.message}")
                Toast.makeText(this@AddTravelActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    inner class TransportModeAdapter(private val items: List<TransportMode>) : RecyclerView.Adapter<TransportModeAdapter.ViewHolder>() {
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) = ViewHolder(LayoutInflater.from(parent.context).inflate(R.layout.item_transport_mode, parent, false))
        override fun onBindViewHolder(holder: ViewHolder, position: Int) = holder.bind(items[position])
        override fun getItemCount() = items.size
        inner class ViewHolder(v: View) : RecyclerView.ViewHolder(v) {
            fun bind(item: TransportMode) {
                itemView.findViewById<LottieAnimationView>(R.id.transport_animation).setAnimation(item.animationResId)
                itemView.findViewById<TextView>(R.id.transport_name).text = item.name
            }
        }
    }
}