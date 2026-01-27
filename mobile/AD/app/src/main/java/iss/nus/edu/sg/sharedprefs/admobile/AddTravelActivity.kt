package iss.nus.edu.sg.sharedprefs.admobile

import android.location.Geocoder
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.RecyclerView
import androidx.viewpager2.widget.CompositePageTransformer
import androidx.viewpager2.widget.ViewPager2
import com.airbnb.lottie.LottieAnimationView
import com.google.android.gms.maps.*
import com.google.android.gms.maps.model.*
import com.google.android.libraries.places.api.Places
import com.google.android.libraries.places.api.model.Place
import com.google.android.libraries.places.widget.AutocompleteSupportFragment
import com.google.android.libraries.places.widget.listener.PlaceSelectionListener
import com.google.android.material.appbar.MaterialToolbar
import java.util.*
import kotlin.math.*

data class TransportMode(val id: Int, val name: String, val animationResId: Int, val emissionFactor: Double)

class AddTravelActivity : AppCompatActivity(), OnMapReadyCallback {

    private lateinit var transportViewPager: ViewPager2
    private lateinit var selectedModeEditText: EditText
    private lateinit var emissionFactorText: TextView
    private lateinit var routeInfoText: TextView
    private lateinit var notesEditText: EditText

    private lateinit var mMap: GoogleMap
    private var startPoint: LatLng? = null
    private var endPoint: LatLng? = null
    private var originAddress = ""
    private var destinationAddress = ""
    private var currentModeId = 6
    private var currentFactor = 0.17

    private val singaporeBounds = LatLngBounds(LatLng(1.130, 103.590), LatLng(1.470, 104.030))

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_travel_activity)

        if (!Places.isInitialized()) {
            // 请确保 API Key 正确
            Places.initialize(applicationContext, "AIzaSyCM7mWNDAl1ZzJ5Kw2eCdSpBwq_gqyNPr0")
        }

        initUI()
        setupSearch() // 修正后的搜索初始化
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
    }

    // 🌟 修改后的搜索逻辑：支持起终点双搜并联动地图
    private fun setupSearch() {
        val fields = listOf(Place.Field.NAME, Place.Field.LAT_LNG, Place.Field.ADDRESS)

        // 1. 获取 Fragment 实例
        val originFragment = supportFragmentManager.findFragmentById(R.id.origin_autocomplete_fragment) as AutocompleteSupportFragment
        val destFragment = supportFragmentManager.findFragmentById(R.id.destination_autocomplete_fragment) as AutocompleteSupportFragment

        // 🌟 2. 深度定制 UI 样式 (移除默认图标和背景)
        val fragments = listOf(originFragment, destFragment)
        fragments.forEach { fragment ->
            fragment.view?.let { view ->
                // 隐藏自带的“放大镜”搜索图标
                view.findViewById<View>(com.google.android.libraries.places.R.id.places_autocomplete_search_button)?.visibility = View.GONE

                // 移除搜索框的默认背景 (使其透明，显示你卡片的白色)
                view.findViewById<View>(com.google.android.libraries.places.R.id.places_autocomplete_search_input)?.background = null

                // 调整文字内边距，让文字靠左对齐，对齐你的指示圆点
                val editText = view.findViewById<EditText>(com.google.android.libraries.places.R.id.places_autocomplete_search_input)
                editText.setPadding(0, editText.paddingTop, editText.paddingRight, editText.paddingBottom)
                editText.textSize = 15f // 调整字号，匹配图片观感
            }
        }

        // 3. 配置起点搜索逻辑
        originFragment.setPlaceFields(fields).setHint("your origin").setCountries("SG")
        originFragment.setOnPlaceSelectedListener(object : PlaceSelectionListener {
            override fun onPlaceSelected(place: Place) {
                place.latLng?.let {
                    mMap.clear()
                    startPoint = null
                    endPoint = null // 搜索起点时重置路径
                    handleLocationInput(it)
                    mMap.animateCamera(CameraUpdateFactory.newLatLngZoom(it, 15f))
                }
            }
            override fun onError(status: com.google.android.gms.common.api.Status) {
                Toast.makeText(this@AddTravelActivity, "Search Error", Toast.LENGTH_SHORT).show()
            }
        })

        // 4. 配置终点搜索逻辑
        destFragment.setPlaceFields(fields).setHint("destination").setCountries("SG")
        destFragment.setOnPlaceSelectedListener(object : PlaceSelectionListener {
            override fun onPlaceSelected(place: Place) {
                place.latLng?.let {
                    if (startPoint == null) {
                        Toast.makeText(this@AddTravelActivity, "请先设置起点", Toast.LENGTH_SHORT).show()
                    } else {
                        handleLocationInput(it) // 自动绘制连线
                        // 自动缩放地图包裹两点
                        val bounds = LatLngBounds.Builder().include(startPoint!!).include(it).build()
                        mMap.animateCamera(CameraUpdateFactory.newLatLngBounds(bounds, 120))
                    }
                }
            }
            override fun onError(status: com.google.android.gms.common.api.Status) {}
        })

        // 🌟 5. 右侧切换按钮逻辑 (可选补全)
        findViewById<ImageView>(R.id.iv_swap_locations)?.setOnClickListener {
            if (startPoint != null && endPoint != null) {
                val tempPoint = startPoint
                startPoint = endPoint
                endPoint = tempPoint

                val tempAddr = originAddress
                originAddress = destinationAddress
                destinationAddress = tempAddr

                // 清除地图重新绘制
                mMap.clear()
                handleLocationInput(startPoint!!)
                handleLocationInput(endPoint!!)
                Toast.makeText(this, "已切换起终点", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // 🌟 保持你原有的 ViewPager2 垂直层叠逻辑
    private fun setupViewPager() {
        val transportModes = listOf(
            TransportMode(6, "Car (Gasoline)", R.raw.transport_sedan, 0.17),
            TransportMode(7, "Car(Electric)", R.raw.transport_sedan2, 0.25),
            TransportMode(4, "Bus", R.raw.transport_bus, 0.03),
            TransportMode(2, "Motorcycle", R.raw.transport_cycling, 0.08),
            TransportMode(3, "Subway", R.raw.transport_train, 0.04),
            TransportMode(5, "Ship", R.raw.transport_ship, 0.15)
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
                super.onPageSelected(position)
                val mode = transportModes[position]
                currentModeId = mode.id
                currentFactor = mode.emissionFactor
                selectedModeEditText.setText(mode.name)
                emissionFactorText.text = String.format("Emission Factor: %.2f kgCO₂e/km", mode.emissionFactor)

                // 切换交通工具时实时刷新碳排放显示
                if (startPoint != null && endPoint != null) {
                    val dist = calculateDistance(startPoint!!, endPoint!!)
                    updateEmissionDisplay(dist)
                }
            }
        })
    }

    override fun onMapReady(googleMap: GoogleMap) {
        mMap = googleMap
        mMap.moveCamera(CameraUpdateFactory.newLatLngZoom(LatLng(1.3521, 103.8198), 11f))
        mMap.setLatLngBoundsForCameraTarget(singaporeBounds)

        mMap.setOnMapClickListener { latLng ->
            if (singaporeBounds.contains(latLng)) {
                handleLocationInput(latLng)
            } else {
                Toast.makeText(this, "Please select within Singapore", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // 🌟 保持你原有的手动点选逻辑
    private fun handleLocationInput(latLng: LatLng) {
        if (startPoint == null) {
            startPoint = latLng
            originAddress = getAddress(latLng)
            mMap.addMarker(MarkerOptions().position(latLng).title("Origin"))
            routeInfoText.text = "Origin: $originAddress. Tap for destination."
        } else if (endPoint == null) {
            endPoint = latLng
            destinationAddress = getAddress(latLng)
            mMap.addMarker(MarkerOptions().position(latLng).title("Destination").icon(BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_AZURE)))
            mMap.addPolyline(PolylineOptions().add(startPoint, endPoint).color(0xFF674fa3.toInt()).width(8f))

            val dist = calculateDistance(startPoint!!, endPoint!!)
            updateEmissionDisplay(dist)
        } else {
            mMap.clear()
            startPoint = latLng
            endPoint = null
            originAddress = getAddress(latLng)
            mMap.addMarker(MarkerOptions().position(latLng).title("Origin"))
            routeInfoText.text = "New Origin set. Tap for destination."
        }
    }

    private fun updateEmissionDisplay(dist: Double) {
        val totalCarbon = dist * currentFactor
        routeInfoText.text = "From: $originAddress\nTo: $destinationAddress\nDist: %.2f km | Carbon: %.2f kg".format(dist, totalCarbon)
    }

    private fun getAddress(latLng: LatLng): String {
        val geocoder = Geocoder(this, Locale.ENGLISH)
        return try {
            val list = geocoder.getFromLocation(latLng.latitude, latLng.longitude, 1)
            if (!list.isNullOrEmpty()) {
                val addr = list[0].getAddressLine(0)
                // 截取简短地址，防止 UI 撑破
                if (addr.length > 40) addr.substring(0, 37) + "..." else addr
            } else "Unknown Location"
        } catch (e: Exception) { "Lat/Lng Point" }
    }

    private fun calculateDistance(s: LatLng, e: LatLng): Double {
        val r = 6371.0
        val dLat = Math.toRadians(e.latitude - s.latitude)
        val dLon = Math.toRadians(e.longitude - s.longitude)
        val a = sin(dLat/2).pow(2) + cos(Math.toRadians(s.latitude)) * cos(Math.toRadians(e.latitude)) * sin(dLon/2).pow(2)
        return r * 2 * atan2(sqrt(a), sqrt(1-a))
    }

    private fun submitData() {
        if (startPoint == null || endPoint == null) {
            Toast.makeText(this, "Please select complete route", Toast.LENGTH_SHORT).show()
            return
        }
        val dist = calculateDistance(startPoint!!, endPoint!!)
        val totalCarbon = dist * currentFactor

        Toast.makeText(this, "Saved: %.2f kgCO₂e".format(totalCarbon), Toast.LENGTH_SHORT).show()
        finish()
    }

    inner class TransportModeAdapter(private val items: List<TransportMode>) : RecyclerView.Adapter<TransportModeAdapter.ViewHolder>() {
        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.item_transport_mode, parent, false)
            return ViewHolder(view)
        }
        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            holder.bind(items[position])
        }
        override fun getItemCount() = items.size
        inner class ViewHolder(v: View) : RecyclerView.ViewHolder(v) {
            fun bind(item: TransportMode) {
                itemView.findViewById<LottieAnimationView>(R.id.transport_animation).setAnimation(item.animationResId)
                itemView.findViewById<TextView>(R.id.transport_name).text = item.name
            }
        }
    }
}